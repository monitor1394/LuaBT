#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NodeCanvas.Framework;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace ParadoxNotion.Design{

	partial class EditorUtils {

		//a special object field for the BBParameter class to let user choose either a constant value or link to a Blackboard Variable.
		public static BBParameter BBParameterField(string prefix, BBParameter bbParam, bool blackboardOnly = false, MemberInfo member = null, object context = null){
			return BBParameterField( string.IsNullOrEmpty(prefix)? GUIContent.none : new GUIContent(prefix), bbParam, blackboardOnly, member, context );
		}
		public static BBParameter BBParameterField(GUIContent content, BBParameter bbParam, bool blackboardOnly = false, MemberInfo member = null, object context = null){

			if (bbParam == null){
				EditorGUILayout.LabelField(content, new GUIContent("Variable is null") );
				return null;
			}

			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();

			//override if we have a memeber info
			if (member != null){
				blackboardOnly = member.RTIsDefined<BlackboardOnlyAttribute>(true);
			}

			//Direct assignement
			if (!blackboardOnly && !bbParam.useBlackboard){

				GUILayout.BeginVertical();
                if (content.text.Equals("Lua Cls"))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Lua Cls");
                    bool isAction = context is LuaAction;
                    string[] options = GetFileList(isAction);
                    int index = 0;
                    if(bbParam.value != null)
                    {
                        for(int i = 0; i < options.Length; i++)
                        {
                            if (options[i].Equals(bbParam.value))
                            {
                                index = i;
                                break;
                            }
                        }
                    }
                    index = EditorGUILayout.Popup(index, options);
                    if (index <= 0) bbParam.value = null;
                    else bbParam.value = options[index];
                    GUILayout.EndHorizontal();
                }
                else
                {
                    bbParam.value = GenericField(content.text, bbParam.value, bbParam.varType, member, context);
                }
				GUILayout.EndVertical();
			
			//Dropdown variable selection
			} else {

				GUI.color = new Color(0.9f,0.9f,1f,1f);
				var varNames = new List<string>();
				
				//Local
				if (bbParam.bb != null){
					varNames.AddRange(bbParam.bb.GetVariableNames(bbParam.varType));
				}

				//Seperator
				varNames.Add("/");

				//Globals
				foreach (var globalBB in GlobalBlackboard.allGlobals) {
				    var globalVars = globalBB.GetVariableNames(bbParam.varType);
				    if (globalVars.Length == 0){
					    varNames.Add(globalBB.name + "/");
				    }
				    for (var i = 0; i < globalVars.Length; i++){
				        globalVars[i] = globalBB.name + "/" + globalVars[i];
				    }
				    varNames.AddRange( globalVars );
				}

				//Dynamic
				varNames.Add("(DynamicVar)");

				//New
				if (bbParam.bb != null){
					varNames.Add("(Create New)");
				}
				
				var isDynamic = !string.IsNullOrEmpty(bbParam.name) && !varNames.Contains(bbParam.name);
				if (!isDynamic){

					bbParam.name = StringPopup(content, bbParam.name, varNames, false, true);
					
					if (bbParam.name == "(DynamicVar)"){
						bbParam.name = "_";
					}

					if (bbParam.bb != null && bbParam.name == "(Create New)"){
						if (bbParam.bb.AddVariable(content.text, bbParam.varType) != null){
							bbParam.name = content.text;
						} else {
							bbParam.name = null;
						}
					}
				
				} else {
					
					bbParam.name = EditorGUILayout.DelayedTextField(content.text + " (" + bbParam.varType.FriendlyName() + ")", bbParam.name);
					GUI.backgroundColor = new Color(1,1,1,0.2f);
					if (bbParam.bb != null && GUILayout.Button(new GUIContent("▲", "Promote To Variable"), GUILayout.Width(20), GUILayout.Height(14))){
						bbParam.PromoteToVariable(bbParam.bb);
					}
				}
			}


			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;

			if (!blackboardOnly){
				bbParam.useBlackboard = EditorGUILayout.Toggle(bbParam.useBlackboard, EditorStyles.radioButton, GUILayout.Width(18));
			}

			GUILayout.EndHorizontal();

			string info = null;

			if (bbParam.useBlackboard){
				if (bbParam.bb == null){
					info = "<i>No current Blackboard reference</i>";
				} else
				if (bbParam.isNone){
					info = "Select a '" + bbParam.varType.FriendlyName() + "' Assignable Blackboard Variable";
				} else
				if (bbParam.varRef != null && bbParam.varType != bbParam.refType){
					if (blackboardOnly){
						info = string.Format("AutoConvert: ({0} ➲ {1})", bbParam.varType.FriendlyName(), bbParam.refType.FriendlyName() );
					} else {
						info = string.Format("AutoConvert: ({0} ➲ {1})", bbParam.refType.FriendlyName(), bbParam.varType.FriendlyName() );
					}
				}
			}

			if (info != null){
				GUI.backgroundColor = new Color(0.8f,0.8f,1f,0.5f);
				GUI.color = new Color(1f,1f,1f,0.5f);
				GUILayout.BeginVertical("textfield");
				GUILayout.Label(info, GUILayout.Width(0), GUILayout.ExpandWidth(true));
				GUILayout.EndVertical();
				GUILayout.Space(2);
			}

			GUILayout.EndVertical();
			GUI.backgroundColor = Color.white;
			GUI.color = Color.white;			
			return bbParam;
		}

        public static string[] GetFileList(bool isAction)
        {
            string dir = "";
            if (isAction) dir = "C:/work/project/LuaBT/LuaBT/ai/actions";
            else dir = "C:/work/project/LuaBT/LuaBT/ai/conditions";
            var files = Directory.GetFiles(dir, "*.lua");
            List<string> fileList = new List<string>();
            fileList.Add("None");
            foreach (var file in files)
            {
                int sindex = file.LastIndexOf("\\");
                int eindex = file.LastIndexOf(".");
                string name = file.Substring(sindex + 1, eindex - sindex - 1);
                fileList.Add(name);
            }
            return fileList.ToArray();
        }
    }
}

#endif