#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using NodeCanvas.BehaviourTrees;
using NodeCanvas.Editor;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEditor;
using UnityEngine;


namespace NodeCanvas.Framework{

	partial class Node{

		//Class for the nodeports GUI
		class GUIPort{
			public int portIndex{get; private set;}
			public Node parent{get; private set;}
			public Vector2 pos{get; private set;}
			public GUIPort(int index, Node parent, Vector2 pos){
				this.portIndex = index;
				this.parent = parent;
				this.pos = pos;
			}
		}

		[SerializeField]
		private bool _collapsed;
		[SerializeField]
		private Color _color;

		private Vector2 size = minSize;
		private object _icon = null;
		private bool colorLoaded{get;set;}
		private bool hasColorAttribute{get;set;}
		private string hexColor{get;set;}
		private Color colorAttributeColor{get;set;}
		private bool nodeIsPressed{get;set;}

		private const string DEFAULT_HEX_COLOR_LIGHT = "eed9a7";
		private const string DEFAULT_HEX_COLOR_DARK = "333333";
		private static GUIPort clickedPort{get;set;}
		private static GUIStyle _centerLabel = null;
		private static int dragDropMisses;

		public static Node[] copiedNodes{get;set;}
		readonly public static Vector2 minSize = new Vector2(100, 20);
		readonly public static Dictionary<Status, Color> statusColors = new Dictionary<Status, Color>()
		{
			{Status.Failure, new Color(1.0f, 0.3f, 0.3f)},
			{Status.Success, new Color(0.4f, 0.7f, 0.2f)},
			{Status.Running, Color.yellow},
			{Status.Resting, new Color(0.7f, 0.7f, 1f, 0.8f)},
			{Status.Error, Color.red},
			{Status.Optional, Color.grey}
		};


		//This is to be able to work with rects which is easier in many cases.
		//Size is temporary to the node since it's auto adjusted thus no need to serialize it
		public Rect nodeRect{
			get { return new Rect(_position.x, _position.y, size.x, size.y); }
			set
			{
				_position = new Vector2((int)value.x, (int)value.y);
				size = new Vector2(Mathf.Max(value.width, minSize.x), Mathf.Max(value.height, minSize.y) );
			}
		}

		///EDITOR! Active is relevant to the input connections
		public bool isActive{
			get
			{
				for (var i = 0; i < inConnections.Count; i++){
					if (inConnections[i].isActive){
						return true;
					}
				}
				return inConnections.Count == 0;
			}
		}

		///EDITOR! Are children collapsed?
		public bool collapsed{
			get {return _collapsed;}
			set {_collapsed = value;}
		}

		///EDITOR! The custom color set by user.
		public Color customColor{
			get {return _color;}
			set {_color = value;}
		}

		///EDITOR! is the node hidden due to parent has children collapsed or is hidden itself?
		public bool isHidden {
			get
			{
				if (graph.autoSort){
					foreach (var parent in inConnections.Select(c => c.sourceNode)){
						if (parent.ID > this.ID){
							continue;
						}
						if (parent.collapsed || parent.isHidden){
							return true;
						}
					}
				}
				return false;
			}
		}

		///EDITOR! is the node selected or part of the multi selection?
		public bool isSelected{
			get {return Graph.currentSelection == this || Graph.multiSelection.Contains(this);}
		}

		//Label style with alignment in center
		private static GUIStyle titleStyle{
			get
			{
				if (_centerLabel == null){
					_centerLabel = new GUIStyle("label");
					_centerLabel.alignment = TextAnchor.MiddleCenter;
					_centerLabel.richText = true;
				}
				return _centerLabel;
			}
		}

		//Is NC in icon mode && node has an icon?
		private bool showIcon{
			get {return NCPrefs.showIcons && icon != null;}
		}

		//The icon of the node
		public Texture2D icon{
			get
			{
				if (_icon == null){
					if (this is ITaskAssignable){
						var assignable = this as ITaskAssignable;
						_icon = assignable.task != null? assignable.task.icon : null;
					}
					if (_icon == null){
						var iconAtt = this.GetType().RTGetAttribute<IconAttribute>(true);
						_icon = iconAtt != null? UserTypePrefs.GetTypeIcon(iconAtt, this) : null;
					}
					if (_icon == null){
						_icon = new object();
					}
				}
				return _icon as Texture2D;
			}
		}


		//The coloring of the node if any.
		public Color nodeColor{
			get
			{
				if (customColor != default(Color)){
					return customColor;
				}

				if (!colorLoaded){
					colorLoaded = true;
					hasColorAttribute = false;
					colorAttributeColor = default(Color);
					hexColor = DEFAULT_HEX_COLOR_LIGHT;
					var cAtt = this.GetType().RTGetAttribute<ColorAttribute>(true);
					if (cAtt != null){
						hasColorAttribute = true;
						colorAttributeColor = cAtt.Resolve();
						hexColor = cAtt.hexColor;
					}
				}
				return colorAttributeColor;
			}
			private set
			{
				if (customColor != value){
					if (value.a <= 0.2f){
						customColor = default(Color);
						hexColor = DEFAULT_HEX_COLOR_LIGHT;
						return;
					}
					customColor = value;
					var temp = (Color32)value;
					hexColor = (temp.r.ToString("X2") + temp.g.ToString("X2") + temp.b.ToString("X2")).ToLower();
				}
			}
		}


		////////////////

		//Helper function to create a nested graph for an IGraphAssignable
		protected static void CreateNested<T>(IGraphAssignable parent) where T:Graph{
			var newGraph = EditorUtils.CreateAsset<T>(true);
			if (newGraph != null){
				Undo.RegisterCreatedObjectUndo(newGraph, "CreateNested");
				parent.nestedGraph = newGraph;
			}
		}

		///Get connection information node wise, to show on top of the connection
		virtual public string GetConnectionInfo(int index){
			return null;
		}

		///Extra inspector controls for the provided OUT connection
		virtual public void OnConnectionInspectorGUI(int index){

		}

		//The main function for drawing a node's gui.Fires off others.
		public void ShowNodeGUI(Rect drawCanvas, bool fullDrawPass, Vector2 canvasMousePos, float zoomFactor){

			if (isHidden){
				return;
			}

			if (fullDrawPass || drawCanvas.Overlaps(nodeRect)){
				DrawNodeWindow(canvasMousePos, zoomFactor);
				DrawNodeTag();
				DrawNodeComments();
				DrawNodeID();
			}

			DrawNodeConnections(drawCanvas, fullDrawPass, canvasMousePos, zoomFactor);
		}

		void DrawNodeID(){
			if (NCPrefs.showNodeIDs){
				var rect = new Rect(nodeRect.x, nodeRect.y - 18, nodeRect.width, 18);
				GUI.color = Color.grey;
				GUI.Label(rect, string.Format("<size=9>#{0}</size>", ID.ToString() ) );
				GUI.color = Color.white;
			}
		}

		//Draw the window
		void DrawNodeWindow(Vector2 canvasMousePos, float zoomFactor){

			///un-colapse children ui
			if (collapsed){
				var r = new Rect(nodeRect.x, (nodeRect.yMax + 10), nodeRect.width, 20);
				EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);
				if (GUI.Button(r, "COLLAPSED", (GUIStyle)"box")){
					collapsed = false;
				}
			}

			GUI.color = isActive? Color.white : new Color(0.9f, 0.9f, 0.9f, 0.8f);
			GUI.color = Graph.currentSelection == this? new Color(0.9f, 0.9f, 1) : GUI.color;
			nodeRect = GUILayout.Window(ID, nodeRect, NodeWindowGUI, string.Empty, (GUIStyle)"window");


			GUI.Box(nodeRect, string.Empty, (GUIStyle)"windowShadow");
			GUI.color = new Color(1,1,1,0.5f);
			GUI.Box(new Rect(nodeRect.x+6, nodeRect.y+6, nodeRect.width, nodeRect.height), string.Empty, (GUIStyle)"windowShadow");

			if (Application.isPlaying && status != Status.Resting){
				GUI.color = statusColors[status];
				GUI.Box(nodeRect, string.Empty, (GUIStyle)"windowHighlight");
				
			} else {
				
				if (isSelected){
					GUI.color = statusColors[Status.Resting];
					GUI.Box(nodeRect, string.Empty, (GUIStyle)"windowHighlight");
				}
			}

			GUI.color = Color.white;
			if (Graph.allowClick){
				if (zoomFactor == 1f){
					EditorGUIUtility.AddCursorRect(new Rect(nodeRect.x, nodeRect.y, nodeRect.width, nodeRect.height ), MouseCursor.Link);
				}
			}
		}


		//This is the callback function of the GUILayout.window. Everything here is called INSIDE the node Window callback.
		//The Window ID is the same as this node's ID.
		void NodeWindowGUI(int ID){

			var e = Event.current;

			ShowHeader();
			ShowPossibleWarningIcon();
			HandleEvents(e);
			ShowStatusIcons();
			ShowBreakpointMark();
			ShowNodeContents();
			HandleContextMenu(e);
			HandleNodePosition(e);
		}

		//The title name or icon of the node
		void ShowHeader(){

			var colorizeHeader = NCPrefs.nodeHeaderStyle == NCPrefs.NodeHeaderStyle.ColorizeHeader;

			//alternative idea where header gets a colored background instead of coloring the text
			if (!showIcon || iconAlignment != Alignment2x2.Default){
				if (name != null){
					var finalTitle = this is IGraphAssignable? string.Format("{{ {0} }}", name) : name;
					var image = showIcon && iconAlignment == Alignment2x2.Left? icon : null;
					string hex;
					var isProSkin = EditorGUIUtility.isProSkin;
					if (colorizeHeader && nodeColor != default(Color)){
						hex = nodeColor.grayscale > 0.6f? DEFAULT_HEX_COLOR_DARK : DEFAULT_HEX_COLOR_LIGHT;
					} else {
						hex = isProSkin? hexColor : DEFAULT_HEX_COLOR_DARK;
					}

					if (colorizeHeader && nodeColor != default(Color)){
						GUI.color = nodeColor;
						var headerHeight = nodeRect.height <= 35? 35 : 27;
						GUI.Box(new Rect(0,0,nodeRect.width, headerHeight), string.Empty, (GUIStyle)"windowHeader");
						GUI.color = Color.white;
					}

					var text = string.Format("<b><size=12><color=#{0}>{1}{2}</color></size></b>", hex, (image != null? " " : ""), finalTitle);
					var content = new GUIContent(text, image, description);
					GUILayout.Label(content, titleStyle, GUILayout.MaxHeight(23));
				}
			}

			
			if (showIcon && (iconAlignment == Alignment2x2.Default || iconAlignment == Alignment2x2.Bottom) ){ //prefs in icon mode AND has icon
				GUI.color = nodeColor.a > 0.2f? nodeColor : Color.white;
				if (!EditorGUIUtility.isProSkin){
					var assignable = this as ITaskAssignable;
					IconAttribute att = null;
					if (assignable != null && assignable.task != null){
						att = assignable.task.GetType().RTGetAttribute<IconAttribute>(true);
					}

					if (att == null){
						att = this.GetType().RTGetAttribute<IconAttribute>(true);
					}

					if (att != null){
						if (att.fixedColor == false){
							GUI.color = new Color(0f,0f,0f, 0.7f);
						}
					}
				}
				
				GUI.backgroundColor = Color.clear;
				GUILayout.Box(icon, GUILayout.MaxHeight(32));
				GUI.backgroundColor = Color.white;
				GUI.color = Color.white;
			}

			//horizontal line node color indicator
			if ( !colorizeHeader && name != null && nodeColor.a > 0.2f && (!showIcon || !hasColorAttribute) ){
				var lastRect = GUILayoutUtility.GetLastRect();
				var hMargin = EditorGUIUtility.isProSkin? 4 : 1;
				GUILayout.Space(2);
				GUI.color = nodeColor;
				GUI.DrawTexture(new Rect(hMargin, lastRect.yMax, nodeRect.width - (hMargin * 2), 3), EditorGUIUtility.whiteTexture);
				GUI.color = Color.white;
			}
		}

		///Responsible for showing a warning icon
		void ShowPossibleWarningIcon(){
			var assignable = this as ITaskAssignable;
			if (assignable != null && assignable.task != null){
				var warning = assignable.task.GetWarning();
				if (warning != null){
			        var errorRect = new Rect(nodeRect.width - 21, 5, 16, 16);
		        	GUI.Box(errorRect, EditorUtils.warningIcon, GUIStyle.none);					
				}
			}
		}

		//Handles events, Mouse downs, ups etc.
		void HandleEvents(Event e){

			//Node click
			if (e.type == EventType.MouseDown && Graph.allowClick && e.button != 2){

	    		Undo.RegisterCompleteObjectUndo(graph, "Move Node");

				if (!e.control){
					Graph.currentSelection = this;
				}

				if (e.control){
					if (isSelected) { Graph.multiSelection.Remove(this); }
					else { Graph.multiSelection.Add(this); }
				}

				if (e.button == 0){
					nodeIsPressed = true;
				}

				//Double click
				if (e.button == 0 && e.clickCount == 2){
		    		if (this is IGraphAssignable && (this as IGraphAssignable).nestedGraph != null ){
	    				graph.currentChildGraph = (this as IGraphAssignable).nestedGraph;
	    				nodeIsPressed = false;
                        BTDebug.SyncSubTree(graph,(this as SubTree).ID);//new add
		    		} else if (this is ITaskAssignable && (this as ITaskAssignable).task != null){
		    			EditorUtils.OpenScriptOfType((this as ITaskAssignable).task.GetType());
		    		} else {
			    		EditorUtils.OpenScriptOfType(this.GetType());
		    		}
		    		e.Use();
		    	}

		    	OnNodePicked();
			}

	    	//Mouse up
	    	if (e.type == EventType.MouseUp){
	    		nodeIsPressed = false;
	    		if (graph.autoSort){
	    			Graph.PostGUI += delegate { SortConnectionsByPositionX(); };
	    		}
	    		OnNodeReleased();
	    	}			
		}

		//Shows the icons relative to the current node status
		void ShowStatusIcons(){
	        if (Application.isPlaying && status != Status.Resting){
		        var markRect = new Rect(5, 5, 15, 15);
		        if (status == Status.Success){
		        	GUI.color = statusColors[Status.Success];
		        	GUI.Box(markRect, string.Empty, (GUIStyle)"checkMark");

		        } else if (status == Status.Running){
		        	GUI.Box(markRect, string.Empty, (GUIStyle)"clockMark");

		        } else if (status == Status.Failure){
		        	GUI.color = statusColors[Status.Failure];
		        	GUI.Box(markRect, string.Empty, (GUIStyle)"xMark");
		        }
		    }
		}

		//Shows the breakpoint mark icon if node is set as a breakpoint
		void ShowBreakpointMark(){
			if (!isBreakpoint){
				return;
			}
			var rect = new Rect(nodeRect.width-15, 5, 12, 12);
			GUI.DrawTexture(rect, EditorUtils.redCircle);
		}

		//Shows the actual node contents GUI
		void ShowNodeContents(){
	        
	        GUI.color = Color.white;
	        GUI.skin = null;
	        GUI.skin.label.richText = true;
	        GUI.skin.label.alignment = TextAnchor.MiddleCenter;

			OnNodeGUI();

			if (this is ITaskAssignable){
				var task = (this as ITaskAssignable).task;
				if (task == null){
					GUILayout.Label("No Task");
				} else {
					GUILayout.Label(NCPrefs.showTaskSummary? task.summaryInfo : string.Format("<b>{0}</b>", task.name));
				}
			}

			GUI.skin.label.alignment = TextAnchor.UpperLeft;
		}

		//Handles and shows the right click mouse button for the node context menu
		void HandleContextMenu(Event e){

			var isContextClick = (e.type == EventType.MouseUp && e.button == 1) || ( /*e.control && e.type == EventType.MouseUp ||*/ e.type == EventType.ContextClick);
		    if (Graph.allowClick && isContextClick){
		    	//Multiselection menu
		    	if (Graph.multiSelection.Count > 1){
		            var menu = new GenericMenu();
		            menu.AddItem (new GUIContent ("Duplicate Selected Nodes"), false, ()=>
		            	{
							var newNodes = Graph.CloneNodes(Graph.multiSelection.OfType<Node>().ToList(), graph);
							Graph.multiSelection = newNodes.Cast<object>().ToList();
		            	});
					menu.AddItem (new GUIContent ("Copy Selected Nodes"), false, ()=>{ copiedNodes = Graph.CloneNodes(Graph.multiSelection.OfType<Node>().ToList() ).ToArray();  });
			        menu = OnContextMenuMulti(menu, Graph.multiSelection.OfType<Node>().ToArray());
			        if (menu != null){
			            menu.AddSeparator("/");
			            menu.AddItem (new GUIContent ("Delete Selected Nodes"), false, ()=>{ foreach (Node node in Graph.multiSelection.ToArray()) graph.RemoveNode(node); });
				        Graph.PostGUI += ()=> { menu.ShowAsContext(); }; //Post GUI cause of zoom
				    }
			        e.Use();
			        return;

		    	//Single node menu
		    	} else {

		            var menu = new GenericMenu();

		            if (graph.primeNode != this && allowAsPrime)
			            menu.AddItem (new GUIContent ("Set Start"), false, ()=> {graph.primeNode = this;});

			        if (this is IGraphAssignable)
			        	menu.AddItem (new GUIContent ("Edit Nested (Double Click)"), false, ()=> {graph.currentChildGraph = (this as IGraphAssignable).nestedGraph; } );

					
					menu.AddItem (new GUIContent ("Duplicate (CTRL+D)"), false, ()=> { Graph.currentSelection = this.Duplicate(graph); });
		            menu.AddItem( new GUIContent("Copy Node"), false, ()=> { copiedNodes = new Node[]{this}; } );

		            if (inConnections.Count > 0)
			            menu.AddItem (new GUIContent (isActive? "Disable" : "Enable"), false, ()=> {SetActive(!isActive);});

					if (graph.autoSort && outConnections.Count > 0)
						menu.AddItem (new GUIContent (collapsed? "Expand Children" : "Collapse Children"), false, ()=> { collapsed = !collapsed; });

					if (this is ITaskAssignable){
						var assignable = this as ITaskAssignable;
						if (assignable.task != null){
							menu.AddItem (new GUIContent("Copy Assigned Task"), false, ()=> { Task.copiedTask = assignable.task; });
						} else {
							menu.AddDisabledItem(new GUIContent("Copy Assigned Task"));
						}

						if (Task.copiedTask != null) {
							menu.AddItem (new GUIContent("Paste Assigned Task"), false, ()=>
							{
								if (assignable.task == Task.copiedTask)
									return;

								if (assignable.task != null){
									if (!EditorUtility.DisplayDialog("Paste Task", string.Format("Node already has a Task assigned '{0}'. Replace assigned task with pasted task '{1}'?", assignable.task.name, Task.copiedTask.name), "YES", "NO")){
										return;
									}
								}

								try {assignable.task = Task.copiedTask.Duplicate(graph);}
								catch {ParadoxNotion.Services.Logger.LogWarning("Can't paste Task here. Incombatible Types", "Editor", this);}
							});

						} else {
							menu.AddDisabledItem(new GUIContent("Paste Assigned Task"));
						}
					}
					
		            menu = OnContextMenu(menu);
		            if (menu != null){
						menu.AddSeparator("/");
			            menu.AddItem (new GUIContent ("Delete (DEL)"), false, ()=> {graph.RemoveNode(this);});
			            Graph.PostGUI += ()=> { menu.ShowAsContext(); };
			        }
		            e.Use();
		    	}
		    }
		}

		//basicaly handles the node position and draging etc
		void HandleNodePosition(Event e){

		    if (Graph.allowClick && e.button != 2){

	    		//drag all selected nodes
	    		if (e.type == EventType.MouseDrag && Graph.multiSelection.Count > 1){
	    			for (var i = 0; i < Graph.multiSelection.Count; i++){
	    				((Node)Graph.multiSelection[i]).nodePosition += e.delta;
	    			}
	    			return;
	    		}

	    		if (nodeIsPressed){

		    		var hierarchicalMove = NCPrefs.hierarchicalMove != e.shift;

		    		//snap to grid
			    	if (!hierarchicalMove && NCPrefs.doSnap && Graph.multiSelection.Count == 0){
			    		nodePosition = new Vector2( Mathf.Round(nodePosition.x/15) * 15, Mathf.Round(nodePosition.y/15) * 15 );
					}		

			    	//recursive drag
			    	if (graph.autoSort && e.type == EventType.MouseDrag){
				    	if (hierarchicalMove || collapsed ){
				    		RecursivePanNode(e.delta);
				    	}
			    	}

		    	}

		    	//this drag
				GUI.DragWindow();
		    }			
		}

		//The comments of the node sitting next or bottom of it
		void DrawNodeComments(){

			if ( !NCPrefs.showComments || string.IsNullOrEmpty(nodeComment)){
				return;
			}

			var commentsRect = new Rect();
			var style = GUI.skin.GetStyle("textArea");
			var size = style.CalcSize(new GUIContent(nodeComment));

			if (commentsAlignment == Alignment2x2.Top){
				size.y = style.CalcHeight(new GUIContent(nodeComment), nodeRect.width);
				commentsRect = new Rect(nodeRect.x, nodeRect.y - size.y, nodeRect.width, size.y -2);
			}
			if (commentsAlignment == Alignment2x2.Bottom){
				size.y = style.CalcHeight(new GUIContent(nodeComment), nodeRect.width);
				commentsRect = new Rect(nodeRect.x, nodeRect.yMax + 5, nodeRect.width, size.y);
			}
			if (commentsAlignment == Alignment2x2.Left){
				var width = Mathf.Min(size.x, nodeRect.width * 2);
				commentsRect = new Rect(nodeRect.xMin - width, nodeRect.yMin, width, nodeRect.height);
			}
			if (commentsAlignment == Alignment2x2.Right){
				commentsRect = new Rect(nodeRect.xMax + 5, nodeRect.yMin, Mathf.Min(size.x, nodeRect.width * 2), nodeRect.height);
			}

			GUI.color = new Color(1,1,1,0.6f);
			GUI.backgroundColor = new Color(1f,1f,1f,0.2f);
			GUI.Box(commentsRect, nodeComment, "textArea");
			GUI.backgroundColor = Color.white;
			GUI.color = Color.white;
		}

		//Shows the tag label on the left of the node if it is tagged
		void DrawNodeTag(){

			if (!string.IsNullOrEmpty(tag)){
				var size = new GUIStyle("label").CalcSize(new GUIContent(tag));
				var tagRect = new Rect(nodeRect.x - size.x -10, nodeRect.y, size.x, size.y);
				GUI.Label(tagRect, tag);
				tagRect.width = EditorUtils.tagIcon.width;
				tagRect.height = EditorUtils.tagIcon.height;
				tagRect.y += tagRect.height + 5;
				tagRect.x = nodeRect.x - 22;
				GUI.DrawTexture(tagRect, EditorUtils.tagIcon);
			}
		}

		//Function to pan the node with children recursively
		void RecursivePanNode(Vector2 delta){

			nodePosition += delta;

			for (var i= 0; i < outConnections.Count; i++){
				var node = outConnections[i].targetNode;
				if (node.ID > this.ID){
					node.RecursivePanNode(delta);
				}
			}
		}

		//The inspector of the node shown in the editor panel or else.
		public void ShowNodeInspectorGUI(){

			UndoManager.CheckUndo(graph, "Node Inspector");

			if (NCPrefs.showNodeInfo){
				GUI.backgroundColor = EditorUtils.lightBlue;
				EditorGUILayout.HelpBox(description, MessageType.None);
				GUI.backgroundColor = Color.white;
			}

			GUILayout.BeginHorizontal();
			if (!showIcon && allowAsPrime){
				customName = EditorGUILayout.TextField(customName);
				EditorUtils.TextFieldComment(customName, "Name...");
			}

			tag = EditorGUILayout.TextField(tag );
			EditorUtils.TextFieldComment(tag, "Tag...");

			if (!hasColorAttribute){
				nodeColor = EditorGUILayout.ColorField(nodeColor, GUILayout.Width(30));
			}

			GUILayout.EndHorizontal();

			GUI.color = new Color(1,1,1,0.5f);
			nodeComment = EditorGUILayout.TextArea(nodeComment);
			GUI.color = Color.white;
			EditorUtils.TextFieldComment(nodeComment, "Comments...");

			EditorUtils.Separator();
			OnNodeInspectorGUI();
			TaskAssignableGUI();

			if (GUI.changed){ //minimize node so that GUILayour brings it back to correct scale
				nodeRect = new Rect( nodeRect.x, nodeRect.y, Node.minSize.x, Node.minSize.y );
			}

			UndoManager.CheckDirty(graph);
		}


		//If the node implements ITaskAssignable this is shown for the user to assign a task
		void TaskAssignableGUI(){

			if (this is ITaskAssignable){

				var assignable = (this as ITaskAssignable);
				System.Type taskType = null;
				foreach(var iType in this.GetType().GetInterfaces()) {
				    if (iType.IsGenericType && iType.GetGenericTypeDefinition() == typeof(ITaskAssignable<>)){
				        taskType = iType.GetGenericArguments()[0];
				        break;
				    }
				}

				if (taskType != null){
					EditorUtils.TaskField(assignable.task, graph, taskType, (t)=> {_icon = null; assignable.task = t;} );
				}
			}
		}


		//Activates/Deactivates all inComming connections
		void SetActive(bool active){

			if (isChecked){
				return;
			}
			
			isChecked = true;

			//just for visual feedback
			if (!active){
				Graph.currentSelection = null;
			}

			Undo.RecordObject(graph, "SetActive");

			//disable all incomming
			foreach (var cIn in inConnections){
				cIn.isActive = active;
			}
			
			//disable all outgoing
			foreach (var cOut in outConnections){
				cOut.isActive = active;
			}

			//if child is still considered active(= at least 1 incomming is active), continue else SetActive child as well
			foreach (var child in outConnections.Select(c => c.targetNode)){
				
				if (child.isActive == !active){
					continue;
				}

				child.SetActive(active);
			}

			isChecked = false;
		}


		//Sorts the connections based on the child nodes and this node X position. Possible only when not in play mode
		void SortConnectionsByPositionX(){
			if (!Application.isPlaying){
				
				if (isChecked){
					return;
				}

				isChecked = true;
				outConnections = outConnections.OrderBy(c => c.targetNode.nodeRect.center.x ).ToList();
				foreach(var connection in inConnections.ToArray()){
					connection.sourceNode.SortConnectionsByPositionX();
				}
				isChecked = false;
			}
		}


		///Draw an automatic editor inspector for this node.
		protected void DrawDefaultInspector(){
			EditorUtils.ShowAutoEditorGUI(this);
		}
		
		//Editor. When the node is picked
		virtual protected void OnNodePicked(){}
		//Editor. When the node is released (mouse up)
		virtual protected void OnNodeReleased(){}
		///Editor. Override to show controls within the node window
		virtual protected void OnNodeGUI(){}
		//Editor. Override to show controls within the inline inspector or leave it to show an automatic editor
		virtual protected void OnNodeInspectorGUI(){ DrawDefaultInspector(); }
		//Editor. Override to add more entries to the right click context menu of the node
		virtual protected GenericMenu OnContextMenu(GenericMenu menu){ return menu; }
		//Editor. Override to add more entries to the right click context menu when multiple nodes are selected
		virtual protected GenericMenu OnContextMenuMulti(GenericMenu menu, Node[] selection){ return menu; }

		//Draw the connections line from this node, to all of its children. This is the default hierarchical tree style. Override in each system's base node class.
		virtual protected void DrawNodeConnections(Rect drawCanvas, bool fullDrawPass, Vector2 canvasMousePos, float zoomFactor){

			var e = Event.current;

			//Receive connections first
			if (clickedPort != null && e.type == EventType.MouseUp && e.button == 0){
		
				if (nodeRect.Contains(e.mousePosition)){
					graph.ConnectNodes(clickedPort.parent, this, clickedPort.portIndex);
					clickedPort = null;
					e.Use();

				} else {

					dragDropMisses ++;

					if (dragDropMisses == graph.allNodes.Count && clickedPort != null){

						var source = clickedPort.parent;
						var index = clickedPort.portIndex;
						var pos = e.mousePosition;						
						clickedPort = null;
						
						System.Action<System.Type> Selected = delegate(System.Type type){
							var newNode = graph.AddNode(type, pos);
							graph.ConnectNodes(source, newNode, index);
							newNode.SortConnectionsByPositionX();
							Graph.currentSelection = newNode;
						};

						var menu = EditorUtils.GetTypeSelectionMenu(graph.baseNodeType, Selected);
						if (zoomFactor == 1 && NCPrefs.useBrowser){
							menu.ShowAsBrowser(string.Format("Add {0} Node", graph.GetType().Name), graph.baseNodeType );
						} else {
							Graph.PostGUI += ()=> { menu.ShowAsContext(); };
						}
						e.Use();							
					}
				}
			}



			if (maxOutConnections == 0){
				return;
			}

			
			if (fullDrawPass || drawCanvas.Overlaps(nodeRect)){

				var nodeOutputBox = new Rect(nodeRect.x, nodeRect.yMax - 4, nodeRect.width, 12);
				GUI.Box(nodeOutputBox, string.Empty, (GUIStyle)"nodePortContainer");

				//draw the ports
				if (outConnections.Count < maxOutConnections || maxOutConnections == -1){
					for (var i = 0; i < outConnections.Count + 1; i++){
						var portRect = new Rect(0, 0, 10, 10);
						portRect.center = new Vector2(((nodeRect.width / (outConnections.Count + 1)) * (i + 0.5f)) + nodeRect.xMin, nodeRect.yMax + 6);
						GUI.Box(portRect, string.Empty, (GUIStyle)"nodePortEmpty");

						if (collapsed){
							continue;
						}

						if (Graph.allowClick){
							//start a connection by clicking a port
							EditorGUIUtility.AddCursorRect(portRect, MouseCursor.ArrowPlus);
							if (e.type == EventType.MouseDown && e.button == 0 && portRect.Contains(e.mousePosition)){
								dragDropMisses = 0;
								clickedPort = new GUIPort(i, this, portRect.center);
								e.Use();
							}
						}
					}
				}
			}


			//draw the new drag&drop connection line
			if (clickedPort != null && clickedPort.parent == this){
				var yDiff = (clickedPort.pos.y - e.mousePosition.y) * 0.5f;
				yDiff = e.mousePosition.y > clickedPort.pos.y? -yDiff : yDiff;
				var tangA = new Vector2(0, yDiff);
				var tangB = tangA * -1;
				Handles.DrawBezier(clickedPort.pos, e.mousePosition, clickedPort.pos + tangA , e.mousePosition + tangB, new Color(0.5f,0.5f,0.8f,0.8f), null, 3);
			}


			//draw all connected lines
			for (var i = 0; i < outConnections.Count; i++){
				
				var connection = outConnections[i];
				if (connection != null){

					var sourcePos = new Vector2(((nodeRect.width / (outConnections.Count + 1)) * (i + 1) ) + nodeRect.xMin, nodeRect.yMax + 6);
					var targetPos = new Vector2(connection.targetNode.nodeRect.center.x, connection.targetNode.nodeRect.y);

					var sourcePortRect = new Rect(0,0,12,12);
					sourcePortRect.center = sourcePos;

					var targetPortRect = new Rect(0,0,15,15);
					targetPortRect.center = targetPos;

					var boundRect = RectUtils.GetBoundRect(sourcePortRect, targetPortRect);
					if (fullDrawPass || drawCanvas.Overlaps(boundRect)){

						GUI.Box(sourcePortRect, string.Empty, (GUIStyle)"nodePortConnected");
				
						if (collapsed || connection.targetNode.isHidden){
							continue;
						}

						connection.DrawConnectionGUI(sourcePos, targetPos);

						if (Graph.allowClick){
							//On right click disconnect connection from the source.
							if (e.type == EventType.ContextClick && sourcePortRect.Contains(e.mousePosition)){
								graph.RemoveConnection(connection);
								e.Use();
								return;
							}

							//On right click disconnect connection from the target.
							if (e.type == EventType.ContextClick && targetPortRect.Contains(e.mousePosition)){
								graph.RemoveConnection(connection);
								e.Use();
								return;
							}
						}
					}

				}
			}
		}

	}
}

#endif
