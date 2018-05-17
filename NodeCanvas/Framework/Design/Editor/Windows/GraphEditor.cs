#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ParadoxNotion;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using CanvasGroup = NodeCanvas.Framework.CanvasGroup;
using Logger = ParadoxNotion.Services.Logger;

namespace NodeCanvas.Editor{

	public class GraphEditor : EditorWindow{

		//the current instance of the opened editor
		public static GraphEditor current;

		//the current graph loaded for editing. Can be a nested graph of the root graph
		public static Graph currentGraph;

		//the root graph that was first opened in the editor
		[System.NonSerialized]
		private Graph _rootGraph;
		private int rootGraphID;

		//the GrapOwner if any, that was used to open the editor and from which to read the rootGraph
		[System.NonSerialized]
		private GraphOwner _targetOwner;
		private int targetOwnerID;

		private Rect canvasRect; //rect within which the graph is drawn (the window)
		private Rect viewRect; //the panning rect that is drawn within canvasRect
		private Rect nodeBounds; //a rect encapsulating all the nodes
		private Rect totalCanvas; //temporary rect usage
		private Rect minimapRect; //rect to show minimap within

		private Event e;
		private GUISkin guiSkin;
		private bool isMultiSelecting;
		private Vector2 selectionStartPos;
		private bool willRepaint = true;
		private bool fullDrawPass = true;
		private Matrix4x4 oldMatrix;
		private bool mouseButton2Down = false;
		private System.Action OnDoPopup;
		private bool isResizingMinimap;
		private bool isDraggingMinimap;

		private float lastUpdateTime;
		private float deltaTime;
		private Vector2? smoothPan      = null;
		private float? smoothZoomFactor = null;
		private Vector2 _panVelocity    = Vector2.one;
		private float _zoomVelocity     = 1;

	    private static bool welcomeShown = false;

		const float TOOLBAR_HEIGHT = 22;
		const float TOP_MARGIN     = 22;
		const float BOTTOM_MARGIN  = 5;
		const int GRID_SIZE        = 15;
		private readonly static Vector2 virtualCenterOffset = new Vector2(-5000, -5000);
		private readonly static Vector2 zoomPoint           = new Vector2(5, TOP_MARGIN);

		private Graph rootGraph{
			get
			{
				if (_rootGraph == null){
					_rootGraph = EditorUtility.InstanceIDToObject(rootGraphID) as Graph;
				}
				return _rootGraph;
			}
			set
			{
				_rootGraph = value;
				rootGraphID = value != null? value.GetInstanceID() : 0;
			}
		}


		private GraphOwner targetOwner{
			get
			{
				if (_targetOwner == null){
					_targetOwner = EditorUtility.InstanceIDToObject(targetOwnerID) as GraphOwner;
				}
				return _targetOwner;
			}
			set
			{
				_targetOwner = value;
				targetOwnerID = value != null? value.GetInstanceID() : 0;
			}
		}

		private Vector2 pan{
			get {return currentGraph != null? Vector2.Min(currentGraph.translation, Vector2.zero) : virtualCenter;}
			set
			{
				if (currentGraph != null){
					var t = currentGraph.translation;
					t = Vector2.Min(value, Vector2.zero);
					if (smoothPan == null){
						t.x = Mathf.Round(t.x); //pixel perfect correction
						t.y = Mathf.Round(t.y); //pixel perfect correction
					}
					currentGraph.translation = t;
				}
			}
		}

		private float zoomFactor{
			get {return currentGraph != null? Mathf.Clamp(currentGraph.zoomFactor, 0.25f, 1f) : 1f; }
			set {if (currentGraph != null) currentGraph.zoomFactor = Mathf.Clamp(value, 0.25f, 1f); }
		}

		private Vector2 virtualPanPosition{
			get {return (pan - virtualCenterOffset) * -1; }
		}

		private Vector2 virtualCenter{
			get {return -virtualCenterOffset + viewRect.size/2;}
		}

		private Vector2 mousePosInCanvas{
			get {return ViewToCanvas(Event.current.mousePosition); }
		}

        private float screenWidth{ //for retina
        	get {return EditorGUIUtility.currentViewWidth;}
        }

        private float screenHeight{ //for consistency
        	get {return Screen.height;}
        }

		void OnEnable(){
			current = this;
	        var canvasIcon = (Texture)Resources.Load("CanvasIcon");
	        titleContent = new GUIContent("Canvas", canvasIcon);

			willRepaint = true;
			fullDrawPass = true;
			wantsMouseMove = true;
			guiSkin = (GUISkin)Resources.Load( EditorGUIUtility.isProSkin? "NodeCanvasSkin" : "NodeCanvasSkinLight" );
			minSize = new Vector2(700, 300);

			#if UNITY_2017_2_OR_NEWER
			EditorApplication.playModeStateChanged -= PlayModeChange;
			EditorApplication.playModeStateChanged += PlayModeChange;
			#else
			EditorApplication.playmodeStateChanged -= PlayModeChange;
			EditorApplication.playmodeStateChanged += PlayModeChange;
			#endif
			Selection.selectionChanged -= OnSelectionChange;
			Selection.selectionChanged += OnSelectionChange;
			Logger.RemoveListener(OnLogMessageReceived);
			Logger.AddListener(OnLogMessageReceived);
		}


		void OnDisable(){
			current = null;
		    welcomeShown = false;
			Graph.currentSelection = null;
			#if UNITY_2017_2_OR_NEWER
			EditorApplication.playModeStateChanged -= PlayModeChange;
			#else
			EditorApplication.playmodeStateChanged -= PlayModeChange;
			#endif
			Selection.selectionChanged -= OnSelectionChange;
			Logger.RemoveListener(OnLogMessageReceived);
		}

		void PlayModeChange
			(
#if UNITY_2017_2_OR_NEWER
			PlayModeStateChange state
#endif
			){
			Graph.currentSelection = null;
			welcomeShown = true;
			willRepaint = true;
			fullDrawPass = true;
		}

		//Listen to Logs and return true if handled
		bool OnLogMessageReceived(Logger.Message msg){
			if (msg.tag == "Editor"){
				ShowNotification(new GUIContent(msg.text));
				return true;
			}
			return false;
		}

		//Whenever the graph we are viewing has changed and after the fact.
		void OnCurrentGraphChanged(){
			UpdateReferencesAndNodeIDs();
			Graph.currentSelection = null;
			willRepaint = true;
			fullDrawPass = true;
			smoothPan = null;
			smoothZoomFactor = null;
		}

		//Update the references for editor convenience.
		void UpdateReferencesAndNodeIDs(){
			rootGraph = targetOwner != null? targetOwner.graph : rootGraph;
			if (rootGraph != null){
				if (targetOwner != null){
					rootGraph.agent = targetOwner;
					rootGraph.blackboard = targetOwner.blackboard;
				}
				rootGraph.UpdateNodeIDs(true);
				rootGraph.UpdateReferences();

				//update refs for the currenlty viewing nested graph as well
				var current = GetCurrentGraph(rootGraph);
				if (targetOwner != null){
					current.agent = targetOwner;
					current.blackboard = targetOwner.blackboard;
				}
				current.UpdateNodeIDs(true);
				current.UpdateReferences();
			}
		}

		[UnityEditor.Callbacks.OnOpenAsset(1)]
		public static bool OpenAsset(int instanceID, int line){
			var target = EditorUtility.InstanceIDToObject(instanceID) as Graph;
			if (target != null){
				GraphEditor.OpenWindow(target);
				return true;
			}
			return false;
		}

		//Open the window without any references
		public static GraphEditor OpenWindow(){
			return OpenWindow(null, null, null);
		}

	    //Opening the window for a graph owner
	    public static GraphEditor OpenWindow(GraphOwner owner){
	    	var window = OpenWindow(owner.graph, owner, owner.blackboard);
	    	window.targetOwner = owner;
	    	return window;
	    }

	    //For opening the window from gui button in the nodegraph's Inspector.
	    public static GraphEditor OpenWindow(Graph newGraph){
	    	return OpenWindow(newGraph, newGraph.agent, newGraph.blackboard);
	    }

	    //Open GraphEditor initializing target graph
	    public static GraphEditor OpenWindow(Graph newGraph, Component agent, IBlackboard blackboard) {
			var window = GetWindow<GraphEditor>();
			window.willRepaint = true;
			window.fullDrawPass = true;
	        window.rootGraph = newGraph;
	        window.targetOwner = null;
		    if (window.rootGraph != null){
		        window.rootGraph.agent = agent;
		        window.rootGraph.blackboard = blackboard;	        	
		        window.rootGraph.currentChildGraph = null;
		        window.rootGraph.UpdateNodeIDs(true);
		        window.rootGraph.UpdateReferences();
		    }

	        Graph.currentSelection = null;
            if (NCPrefs.showWelcomeWindow && !Application.isPlaying && welcomeShown == false){
                welcomeShown = true;
				var graphType = newGraph != null? newGraph.GetType() : null;
                WelcomeWindow.ShowWindow(graphType);
            }

            return window;
	    }

		//Change viewing graph based on Graph or GraphOwner
		void OnSelectionChange(){
			
			if (NCPrefs.isLocked){
				return;
			}

			if (Selection.activeObject is GraphOwner){
				var lastEditor = EditorWindow.focusedWindow;
				OpenWindow( (GraphOwner)Selection.activeObject );
				if (lastEditor) lastEditor.Focus();
				return;				
			}

			if (Selection.activeObject is Graph){
				var lastEditor = EditorWindow.focusedWindow;
				OpenWindow( (Graph)Selection.activeObject );
				if (lastEditor) lastEditor.Focus();
				return;
			}

			if (Selection.activeGameObject != null){
				var foundOwner = Selection.activeGameObject.GetComponent<GraphOwner>();
				if (foundOwner != null){
					var lastEditor = EditorWindow.focusedWindow;
					OpenWindow(foundOwner);
					if (lastEditor) lastEditor.Focus();
				}
			}
		}


		void Update(){
			var currentTime = Time.realtimeSinceStartup;
			deltaTime = currentTime - lastUpdateTime;
			lastUpdateTime = currentTime;

			DoSmoothPan();
			DoSmoothZoom();
		}

		void DoSmoothPan(){

			if (smoothPan == null){
				return;
			}
			
			var targetPan = (Vector2)smoothPan;
			if ( (targetPan - pan).magnitude < 0.1f ){
				smoothPan = null;
				return;
			}

			targetPan = new Vector2(Mathf.FloorToInt(targetPan.x), Mathf.FloorToInt(targetPan.y));
			pan = Vector2.SmoothDamp(pan, targetPan, ref _panVelocity, 0.05f, Mathf.Infinity, deltaTime);
			Repaint();
		}

		void DoSmoothZoom(){
			
			if (smoothZoomFactor == null){
				return;
			}

			var targetZoom = (float)smoothZoomFactor;
			if ( Mathf.Abs(targetZoom - zoomFactor) < 0.00001f ){
				smoothZoomFactor = null;
				return;
			}
				
			zoomFactor = Mathf.SmoothDamp(zoomFactor, targetZoom, ref _zoomVelocity, 0.05f, Mathf.Infinity, deltaTime);
			if (zoomFactor > 0.99999f){ zoomFactor = 1; }
			Repaint();
		}

		//GUI space to canvas space
		Vector2 ViewToCanvas(Vector2 viewPos){
			return (viewPos - pan)/zoomFactor;
		}

		//Canvas space to GUI space
		Vector2 CanvasToView(Vector2 canvasPos){
			return (canvasPos * zoomFactor) + pan;
		}

		//Show modal quick popup
		void DoPopup(System.Action Call){
			OnDoPopup = Call;
		}

		//Just so that there is some repainting going on
		void OnInspectorUpdate(){
			if (!willRepaint){
				Repaint();
			}
		}

		void OnGUI(){

			if (guiSkin == null){
				guiSkin = (GUISkin)Resources.Load( EditorGUIUtility.isProSkin? "NodeCanvasSkin" : "NodeCanvasSkinLight" );
			}

			if (EditorApplication.isCompiling){
				ShowNotification(new GUIContent("...Compiling Please Wait..."));
				willRepaint = true;
				return;			
			}

			//Init
			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;
			e = Event.current;
			GUI.skin.label.richText = true;
			GUI.skin = guiSkin;

			//get the graph from the GraphOwner if one is set
			if (targetOwner != null){
				rootGraph = targetOwner.graph;
			}

			if (rootGraph == null){
				ShowEmptyGraphGUI();
				return;
			}

			if (e.type == EventType.MouseDown){
				RemoveNotification();
			}

			//set the currently viewing graph by getting the current child graph from the root graph recursively
			var curr = GetCurrentGraph(rootGraph);
			if (!ReferenceEquals(curr, currentGraph)){
				currentGraph = curr;
				OnCurrentGraphChanged();
			}

			if (currentGraph == null || ReferenceEquals(currentGraph, null)){
				return;
			}

			//handle undo/redo keyboard commands
			if (e.type == EventType.ValidateCommand && e.commandName == "UndoRedoPerformed"){
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
                Graph.currentSelection = null;
                willRepaint = true;
                fullDrawPass = true;
                UpdateReferencesAndNodeIDs();
                currentGraph.Validate();
                e.Use();
				return;
			}

			///should we set dirty? Put in practise at the end
			var willDirty = false;
			if (
				(e.rawType == EventType.MouseUp && e.button != 2) ||
				(e.type == EventType.DragPerform) ||
				(e.type == EventType.KeyUp && (e.keyCode == KeyCode.Return || GUIUtility.keyboardControl == 0 ) )
				)
			{
				willDirty = true;
			}

			//initialize rects
			canvasRect = Rect.MinMaxRect(5, TOP_MARGIN, position.width -5, position.height - BOTTOM_MARGIN);
			minimapRect = Rect.MinMaxRect(canvasRect.xMax - NCPrefs.minimapSize.x, canvasRect.yMax - NCPrefs.minimapSize.y, canvasRect.xMax - 2, canvasRect.yMax - 2);
			var originalCanvasRect = canvasRect;
			//handle minimap
			HandleMinimapEvents(e, minimapRect);
			//handles mouse & keyboard inputs
			HandleGraphEvents(e);

			//canvas background
			GUI.Box(canvasRect, string.Empty, (GUIStyle)"canvasBG");

			//backgroundg grid
			DrawGrid(canvasRect, pan, zoomFactor);

			if (zoomFactor != 1){
				canvasRect = StartZoomArea(canvasRect);
			}

			//main group
			GUI.BeginGroup(canvasRect);
			{
				//pan the view rect
				totalCanvas = canvasRect;
				totalCanvas.x = 0;
				totalCanvas.y = 0;
				totalCanvas.x += pan.x/zoomFactor;
				totalCanvas.y += pan.y/zoomFactor;
				totalCanvas.width -= pan.x/zoomFactor;
				totalCanvas.height -= pan.y/zoomFactor;

				//begin panning group
				GUI.BeginGroup(totalCanvas);
				{
					//inverse pan the view rect
					viewRect = totalCanvas;
					viewRect.x = 0;
					viewRect.y = 0;
					viewRect.x -= pan.x/zoomFactor;
					viewRect.y -= pan.y/zoomFactor;
					viewRect.width += pan.x/zoomFactor;
					viewRect.height += pan.y/zoomFactor;

					DoCanvasGroups(e);

					BeginWindows();
					currentGraph.ShowNodesGUI(e, viewRect, fullDrawPass, mousePosInCanvas, zoomFactor);
					EndWindows();

					DoCanvasRectSelection(viewRect);
				}
			
				GUI.EndGroup();
			}

			GUI.EndGroup();

			if (zoomFactor != 1){
				EndZoomArea();
			}

			//minimap
			DrawMinimap(minimapRect);

			//Breadcrumb navigation
			GUILayout.BeginArea(new Rect(20, TOP_MARGIN + 5, screenWidth, screenHeight));
			ShowBreadCrumbNavigation(rootGraph);
			GUILayout.EndArea();


			//Graph controls (after windows so that panels (inspector, blackboard) show on top)
			currentGraph.ShowGraphControls(e, mousePosInCanvas);

			//dirty?
			if (willDirty){
				willDirty = false;
				willRepaint = true;
				currentGraph.Serialize();
				EditorUtility.SetDirty(currentGraph);
			}
			

			//repaint?
			if (willRepaint || e.type == EventType.MouseMove || rootGraph.isRunning ){
				Repaint();
			}

			if (e.type == EventType.Repaint){
				fullDrawPass = false;
				willRepaint = false;
			}



			//playmode indicator
			if (Application.isPlaying){
				var r = new Rect(0, 0, 120, 10);
				r.center = new Vector2(screenWidth/2, screenHeight - BOTTOM_MARGIN - 50);
				GUI.color = Color.green;
				GUI.Box(r, "PlayMode Active", (GUIStyle)"windowHighlight");
			}

			//hack for quick popups
			if (OnDoPopup != null){
				var temp = OnDoPopup;
				OnDoPopup = null;
				QuickPopup.Show(temp);
			}

			//closure
			GUI.Box(originalCanvasRect, string.Empty, (GUIStyle)"canvasBorders");
			GUI.skin = null;
			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;
		}

		//Recursively get the currenlty showing nested graph starting from the root
		Graph GetCurrentGraph(Graph root){
			if (root.currentChildGraph == null){
				return root;
			}
			return GetCurrentGraph(root.currentChildGraph);
		}

		//Starts a zoom area, returns the scaled container rect
		Rect StartZoomArea(Rect container){
			GUI.EndGroup();
			container.height += TOOLBAR_HEIGHT;
			container.width *= 1/zoomFactor;
			container.height *= 1/zoomFactor;
			oldMatrix = GUI.matrix;
			var matrix1 = Matrix4x4.TRS( zoomPoint, Quaternion.identity, Vector3.one );
			var matrix2 = Matrix4x4.Scale(new Vector3(zoomFactor, zoomFactor, 1f));
			GUI.matrix = matrix1 * matrix2 * matrix1.inverse * GUI.matrix;
			return container;
		}

		//Ends the zoom area
		void EndZoomArea(){
			GUI.matrix = oldMatrix;
			var zoomRecoveryRect = new Rect(0, TOOLBAR_HEIGHT, screenWidth, screenHeight);
			#if !UNITY_2017_2_OR_NEWER
			zoomRecoveryRect.y -= 3; //i honestly dont know what that 3 is, but fixes a 3 px dislocation. Unity seems to have fixed it in 2017.
			#endif
			GUI.BeginGroup(zoomRecoveryRect); //Recover rect
		}


		///Graph events
		void HandleGraphEvents(Event e){

			//set repaint counter if need be
			if (mouseOverWindow == this && (e.isMouse || e.isKey) ){
				willRepaint = true;
			}

			//snap all nodes on assumption change
			if (e.type == EventType.MouseUp || e.type == EventType.KeyUp){
				SnapNodesToGrid();
			}

			if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F && GUIUtility.keyboardControl == 0){
				FocusSelection();
			}

			if (e.type == EventType.MouseDown && e.button == 2 && e.clickCount == 2){
				FocusPosition( ViewToCanvas(e.mousePosition) );
			}

			if (e.type == EventType.ScrollWheel && Graph.allowClick){
				if (canvasRect.Contains(e.mousePosition)){
					var zoomDelta = e.shift? 0.1f : 0.25f;
					ZoomAt(e.mousePosition, -e.delta.y > 0? zoomDelta : -zoomDelta );
				}
			}

			if ( (e.button == 2 && e.type == EventType.MouseDrag && canvasRect.Contains(e.mousePosition))
				|| ( (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.alt && e.isMouse) )
			{
				pan += e.delta;
				smoothPan = null;
				smoothZoomFactor = null;
				e.Use();
			}

			if (e.type == EventType.MouseDown && e.button == 2 && canvasRect.Contains(e.mousePosition)){
				mouseButton2Down = true;
			}

			if (e.type == EventType.MouseUp && e.button == 2){
				mouseButton2Down = false;
			}

			if (mouseButton2Down == true || e.alt){
				EditorGUIUtility.AddCursorRect (new Rect(0,0,screenWidth,screenHeight), MouseCursor.Pan);
			}
		}

		
		///Translate the graph to focus selection
		void FocusSelection(){
			if (Graph.multiSelection != null && Graph.multiSelection.Count > 0){
				FocusPosition(GetNodeBounds(Graph.multiSelection.Cast<Node>().ToList(), viewRect, false).center);
				return;
			}
			if (Graph.selectedNode != null){
				FocusNode(Graph.selectedNode);
				return;
			}
			if (Graph.selectedConnection != null){
				FocusConnection(Graph.selectedConnection);
				return;
			}
			if (currentGraph.allNodes.Count > 0){
				FocusPosition(GetNodeBounds(currentGraph.allNodes, viewRect, false).center);
				return;
			}
			FocusPosition(virtualCenter);
		}

		///Translate the graph to the center of the target node
		public void FocusNode(Node node){
			FocusPosition(node.nodeRect.center);
		}

		///Translate the graph to the center of the target connection
		public void FocusConnection(Connection connection){
			var bound = RectUtils.GetBoundRect(connection.sourceNode.nodeRect, connection.targetNode.nodeRect);
			FocusPosition(bound.center);
		}

		///Translate the graph to to center of the target pos
		void FocusPosition(Vector2 targetPos, bool smooth = true){
			if (smooth){
				smoothPan = -targetPos;
				smoothPan += new Vector2( viewRect.width/2, viewRect.height/2);
				smoothPan *= zoomFactor;
			} else {
				pan = -targetPos;
				pan += new Vector2( viewRect.width/2, viewRect.height/2);
				pan *= zoomFactor;
				smoothPan = null;
				smoothZoomFactor = null;				
			}
		}

		///Zoom with center position
		void ZoomAt(Vector2 center, float delta){
			var pinPoint = (center - pan)/zoomFactor;
			var newZ = zoomFactor;
			newZ += delta;
			newZ = Mathf.Clamp(newZ, 0.25f, 1f);
			smoothZoomFactor = newZ;
			var a = (pinPoint * newZ) + pan;
			var b = center;
			var diff = b - a;
			smoothPan = pan + diff;
		}

	
		///Gets the bound rect for the nodes
		Rect GetNodeBounds(List<Node> nodes, Rect container, bool expandToContainer = false){
			if (nodes == null || nodes.Count == 0){
				return container;
			}

			var arr = new Rect[nodes.Count];
			for (var i = 0; i < nodes.Count; i++){
				arr[i] = nodes[i].nodeRect;
			}
			var result = RectUtils.GetBoundRect(arr);
			if (expandToContainer){
				result = RectUtils.GetBoundRect(result, container);
			}
			return result;
		}


		//Do graphical multi selection box for nodes
		void DoCanvasRectSelection(Rect container){
			
			if (Graph.allowClick && e.type == EventType.MouseDown && e.button == 0 && !e.alt && !e.shift && canvasRect.Contains(CanvasToView(e.mousePosition)) ){
				Graph.currentSelection = null;
				selectionStartPos = e.mousePosition;
				isMultiSelecting = true;
				e.Use();
			}

			if (isMultiSelecting && e.rawType == EventType.MouseUp){
				var rect = RectUtils.GetBoundRect(selectionStartPos, e.mousePosition);
				var overlapedNodes = currentGraph.allNodes.Where(n => rect.Overlaps(n.nodeRect) && !n.isHidden).ToList();
				isMultiSelecting = false;

				if (e.control && rect.width > 50 && rect.height > 50){
					Undo.RegisterCompleteObjectUndo(currentGraph, "Create Group");
					if (currentGraph.canvasGroups == null){
						currentGraph.canvasGroups = new List<CanvasGroup>();
					}
					currentGraph.canvasGroups.Add( new CanvasGroup(rect, "New Canvas Group") );
				} else {
					if (overlapedNodes.Count > 0){
						Graph.multiSelection = overlapedNodes.Cast<object>().ToList();
						e.Use();
					}
				}
			}

			if (isMultiSelecting){
				var rect = RectUtils.GetBoundRect(selectionStartPos, e.mousePosition);
				if (rect.width > 5 && rect.height > 5){
					GUI.color = new Color(0.5f,0.5f,1,0.3f);
					GUI.Box(rect, string.Empty);
					foreach (var node in currentGraph.allNodes){
						if (rect.Overlaps(node.nodeRect) && !node.isHidden){
							var highlightRect = node.nodeRect;
							GUI.Box(highlightRect, string.Empty, "windowHighlight");
						}
					}
					if (rect.width > 50 && rect.height > 50){
						GUI.color = new Color(1,1,1, e.control? 0.6f : 0.15f);
						GUI.Label(new Rect( e.mousePosition.x + 16, e.mousePosition.y, 120, 22 ), "<i>+ control for group</i>");
					}
				}
			}

			GUI.color = Color.white;
		}



		//Draw a simple grid
		void DrawGrid(Rect container, Vector2 offset, float zoomFactor){
			
			if (Event.current.type != EventType.Repaint){
				return;
			}

			GL.Begin(GL.LINES);

			var drawGridSize = zoomFactor > 0.5f? GRID_SIZE : GRID_SIZE * 5;
			var step = drawGridSize * zoomFactor;
			
			var xDiff = offset.x % step;
			var xStart = container.xMin + xDiff;
			var xEnd = container.xMax;
			for (var i = xStart; i < xEnd; i += step){
				var high = false;
				GL.Color( new Color(0,0,0, high? 0.5f : 0.1f) );
				GL.Vertex( new Vector3(i, container.yMin, 0) );
				GL.Vertex( new Vector3(i, container.yMax, 0) );
			}

			var yDiff = offset.y % step;
			var yStart = container.yMin + yDiff;
			var yEnd = container.yMax;
			for (var i = yStart; i < yEnd; i += step){
				var high = false;
				GL.Color( new Color(0,0,0, high? 0.5f : 0.1f) );
				GL.Vertex( new Vector3(0, i, 0) );
				GL.Vertex( new Vector3( container.xMax, i, 0) );
			}

			GL.End();
		}


		//This is the hierarchy shown at top left. Recusrsively show the nested path
		void ShowBreadCrumbNavigation(Graph root){

			if (root == null){
				return;
			}

			//if something selected the inspector panel shows on top of the breadcrub. If external inspector active it doesnt matter, so draw anyway.
			if (Graph.currentSelection != null && !NCPrefs.useExternalInspector){
				return;
			}

			var boundInfo = "Bound";
			var prefabType = targetOwner != null? PrefabUtility.GetPrefabType(targetOwner) : PrefabType.None;
			if (prefabType == PrefabType.Prefab){ boundInfo += " Prefab Asset"; }
			if (prefabType == PrefabType.PrefabInstance){ boundInfo += " Prefab Instance"; }
			var assetInfo = EditorUtility.IsPersistent(root)? "Asset Reference" : "Instance";
			var graphInfo = string.Format("<color=#ff4d4d>({0})</color>", targetOwner != null && targetOwner.graph == root && targetOwner.graphIsBound? boundInfo : assetInfo );

			GUI.color = new Color(1f,1f,1f,0.5f);
			
			GUILayout.BeginVertical();
			if (root.currentChildGraph == null){

				if (root.agent == null && root.blackboard == null){
					GUILayout.Label(string.Format("<b><size=22>{0} {1}</size></b>", root.name, graphInfo));	
				} else {
					var agentInfo = root.agent != null? root.agent.gameObject.name : "No Agent";
					var bbInfo = root.blackboard != null? root.blackboard.name : "No Blackboard";
					GUILayout.Label(string.Format("<b><size=22>{0} {1}</size></b>\n<size=10>{2} | {3}</size>", root.name, graphInfo, agentInfo, bbInfo));
				}

			} else {

				GUILayout.BeginHorizontal();

				//"button" implemented this way due to e.used. It's a weird matter..
				GUILayout.Label("⤴ " + root.name, (GUIStyle)"button");
				if (Event.current.type == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)){
					root.currentChildGraph = null;
                    BTDebug.SyncSubTree(root,0); //new add
				}

				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				ShowBreadCrumbNavigation(root.currentChildGraph);
			}

			GUILayout.EndVertical();
			GUI.color = Color.white;
		}

		
		private static Node[] tempGroupNodes;
		private static CanvasGroup[] tempNestedGroups;
		void DoCanvasGroups(Event e){

			if (currentGraph.canvasGroups == null){
				return;
			}

			for (var i = 0; i < currentGraph.canvasGroups.Count; i++){
				var group = currentGraph.canvasGroups[i];
				var handleRect = new Rect( group.rect.x, group.rect.y, group.rect.width, 25);
				var scaleRect = new Rect( group.rect.xMax - 20, group.rect.yMax -20, 20, 20);
				var style = (GUIStyle)"editorPanel";
				
				GUI.color = EditorGUIUtility.isProSkin? new Color(1,1,1,0.4f) : new Color(0.5f,0.5f,0.5f,0.3f);
				GUI.Box(group.rect, string.Empty, style);

				if (group.color != default(Color)){
					GUI.color = group.color;
					GUI.DrawTexture(group.rect, EditorGUIUtility.whiteTexture);
				}

				GUI.color = Color.white;
				GUI.Box(new Rect(scaleRect.x+10, scaleRect.y+10, 6,6), string.Empty, (GUIStyle)"scaleArrow" );
				
				var size = 14 / zoomFactor;
				var name = string.Format("<size={0}>{1}</size>", size, group.name);
				GUI.Label(handleRect, name, style);

				EditorGUIUtility.AddCursorRect(handleRect, group.isRenaming? MouseCursor.Text : MouseCursor.Link);
				EditorGUIUtility.AddCursorRect(scaleRect, MouseCursor.ResizeUpLeft);

				if (group.isRenaming){
					GUI.SetNextControlName("GroupRename");
					group.name = EditorGUI.TextField(handleRect, group.name, style);
					GUI.FocusControl("GroupRename");
					if (e.keyCode == KeyCode.Return || (e.type == EventType.MouseDown && !handleRect.Contains(e.mousePosition)) ){
						group.isRenaming = false;
		                GUIUtility.hotControl = 0;
		                GUIUtility.keyboardControl = 0;
					}
				}

				if (e.type == EventType.MouseDown && Graph.allowClick){

					if (handleRect.Contains(e.mousePosition)){

						Undo.RegisterCompleteObjectUndo(currentGraph, "Move Canvas Group");

						//calc group nodes
						tempGroupNodes = currentGraph.allNodes.Where(n => group.rect.Encapsulates(n.nodeRect) ).ToArray();
						tempNestedGroups = currentGraph.canvasGroups.Where(c => group.rect.Encapsulates(c.rect) ).ToArray();

						if (e.button == 1){
							var menu = new GenericMenu();
							menu.AddItem(new GUIContent("Rename"), false, ()=> { group.isRenaming = true; } );
							menu.AddItem(new GUIContent("Edit Color"), false, ()=> { DoPopup( ()=>
								{
									group.color = EditorGUILayout.ColorField(group.color);
								} ); } );
							menu.AddItem(new GUIContent("Select Nodes"), false, ()=> { Graph.multiSelection = tempGroupNodes.Cast<object>().ToList(); } );
							menu.AddItem(new GUIContent("Delete Group"), false, ()=> { currentGraph.canvasGroups.Remove(group); } );
							Graph.PostGUI += ()=> { menu.ShowAsContext(); };
						} else if (e.button == 0){
							group.isDragging = true;
						}

						e.Use();
					}

					if (e.button == 0 && scaleRect.Contains(e.mousePosition)){
						Undo.RegisterCompleteObjectUndo(currentGraph, "Scale Canvas Group");
						group.isRescaling = true;
						e.Use();
					}
				}

				if (e.rawType == EventType.MouseUp){
					group.isDragging = false;
					group.isRescaling = false;
				}

				if (e.type == EventType.MouseDrag){

					if (group.isDragging){

						group.rect.x += e.delta.x;
						group.rect.y += e.delta.y;						

						if (!e.shift){
							for (var j = 0; j < tempGroupNodes.Length; j++){
								tempGroupNodes[j].nodePosition += e.delta;
							}

							for (var j = 0; j < tempNestedGroups.Length; j++){
								tempNestedGroups[j].rect.x += e.delta.x;
								tempNestedGroups[j].rect.y += e.delta.y;
							}
						}
					}

					if (group.isRescaling){
						group.rect.xMax = Mathf.Max(e.mousePosition.x + 5, group.rect.x + 100);
						group.rect.yMax = Mathf.Max(e.mousePosition.y + 5, group.rect.y + 100);
					}
				}
			}
		}


		//Snap all nodes
		void SnapNodesToGrid(){

			if (!NCPrefs.doSnap){
				return;
			}

			foreach (var node in currentGraph.allNodes){
				var pos = node.nodePosition;
				pos.x = Mathf.Round(pos.x / GRID_SIZE) * GRID_SIZE;
				pos.y = Mathf.Round(pos.y / GRID_SIZE) * GRID_SIZE;
				node.nodePosition = pos;
			}
		}


		//before nodes for handling events
		void HandleMinimapEvents(Event e, Rect container){
			if (!Graph.allowClick){ return; }
			var resizeRect = new Rect(container.x, container.y, 6, 6);
			EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeUpLeft);
			if (e.type == EventType.MouseDown && e.button == 0 && resizeRect.Contains(e.mousePosition)){
				isResizingMinimap = true;
				e.Use();
			}
			if (e.rawType == EventType.MouseUp){
				isResizingMinimap = false;
			}
			if (isResizingMinimap && e.type == EventType.MouseDrag){
				NCPrefs.minimapSize -= e.delta;
				e.Use();
			}

			EditorGUIUtility.AddCursorRect(container, MouseCursor.MoveArrow);
			if (e.type == EventType.MouseDown && e.button == 0 && container.Contains(e.mousePosition)){
				var finalBound = ResolveMinimapBoundRect(currentGraph, viewRect);
				var norm = Rect.PointToNormalized(container, e.mousePosition);
				var pos = Rect.NormalizedToPoint(finalBound, norm);
				FocusPosition(pos);
				isDraggingMinimap = true;
				e.Use();
			}
			if (e.rawType == EventType.MouseUp){
				isDraggingMinimap = false;
			}
			if (isDraggingMinimap && e.type == EventType.MouseDrag){
				var finalBound = ResolveMinimapBoundRect(currentGraph, viewRect);
				var norm = Rect.PointToNormalized(container, e.mousePosition);
				var pos = Rect.NormalizedToPoint(finalBound, norm);
				FocusPosition(pos);
				e.Use();
			}
		}

		///after nodes, a cool minimap
		void DrawMinimap(Rect container){

			GUI.color = new Color(0.5f,0.5f,0.5f,0.85f);
			GUI.Box(container, "", (GUIStyle)"windowShadow");
			GUI.Box(container, currentGraph.allNodes.Count > 0? string.Empty : "Minimap");
			var finalBound = ResolveMinimapBoundRect(currentGraph, viewRect);
			var lensRect = viewRect.TransformSpace(finalBound, container);
			GUI.color = new Color(1,1,1,0.8f);
			GUI.Box(lensRect, string.Empty);
			GUI.color = Color.white;
			finalBound = finalBound.ExpandBy(25);
			if (currentGraph.canvasGroups != null){
				for (var i = 0; i < currentGraph.canvasGroups.Count; i++){
					var group = currentGraph.canvasGroups[i];
					var blipRect = group.rect.TransformSpace(finalBound, container);
					var blipHeaderRect = Rect.MinMaxRect(blipRect.xMin, blipRect.yMin, blipRect.xMax, blipRect.yMin + 2);
					var color = group.color != default(Color)? group.color : Color.gray;
					color.a = 0.5f;
					GUI.color = color;
					GUI.DrawTexture(blipRect, Texture2D.whiteTexture);
					GUI.DrawTexture(blipHeaderRect, Texture2D.whiteTexture);
					GUI.color = Color.white;					
				}
			}
			if (currentGraph.allNodes != null){
				for (var i = 0; i < currentGraph.allNodes.Count; i++){
					var node = currentGraph.allNodes[i];
					if (node.isHidden){ continue; }
					var blipRect = node.nodeRect.TransformSpace(finalBound, container);
					var color = node.nodeColor != default(Color)? node.nodeColor : Color.grey;
					GUI.color = color;
					GUI.DrawTexture(blipRect, Texture2D.whiteTexture);
					GUI.color = Color.white;
				}
			}

			var resizeRect = new Rect(container.x, container.y, 6, 6);
			GUI.color = Color.white;
			GUI.Box(resizeRect, string.Empty, (GUIStyle)"scaleArrowTL");
		}

		//resolves the bounds used in the minimap
		static Rect ResolveMinimapBoundRect(Graph graph, Rect container){
			var arr1 = new Rect[graph.allNodes.Count];
			for (var i = 0; i < graph.allNodes.Count; i++){
				arr1[i] = graph.allNodes[i].nodeRect;
			}

			var nBounds = RectUtils.GetBoundRect(arr1);
			var finalBound = nBounds;

			if (graph.canvasGroups != null && graph.canvasGroups.Count > 0){
				var arr2 = new Rect[graph.canvasGroups.Count];
				for (var i = 0; i < graph.canvasGroups.Count; i++){
					arr2[i] = graph.canvasGroups[i].rect;
				}
				var gBounds = RectUtils.GetBoundRect(arr2);
				finalBound = RectUtils.GetBoundRect(nBounds, gBounds);
			}

			finalBound = RectUtils.GetBoundRect(finalBound, container);
			return finalBound;
		}

		//an idea but it's taking up space i dont like
		void ShowConsoleLog(){
			var rect = Rect.MinMaxRect(canvasRect.xMin, canvasRect.yMax + 5, canvasRect.xMax, screenHeight - TOOLBAR_HEIGHT);
			var msg = GraphConsole.GetFirstMessageForGraph(currentGraph);
			if (msg.IsValid()){
				rect.xMin += 2;
				GUI.Label(rect, GraphConsole.GetFormatedGUIContentForMessage(msg));
				EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
				if (GUI.Button(rect, string.Empty, GUIStyle.none)){
					GraphConsole.ShowWindow();
				}
			}
		}

		//this is shown when root graph is null
		void ShowEmptyGraphGUI(){
			if (targetOwner != null){
				var text = string.Format("The selected {0} does not have a {1} assigned.\n Please create or assign a new one in it's inspector.", targetOwner.GetType().Name, targetOwner.graphType.Name);
				ShowNotification(new GUIContent(text));
				return;
			}
			ShowNotification(new GUIContent("Please select a GraphOwner GameObject or a Graph Asset."));			
		}

	}
}

#endif