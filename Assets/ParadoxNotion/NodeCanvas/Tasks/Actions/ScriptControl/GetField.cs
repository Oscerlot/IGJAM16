﻿using System.Reflection;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;
using System.Linq;

namespace NodeCanvas.Tasks.Actions{

	[Category("✫ Script Control/Common")]
	[Description("Get a variable of a script and save it to the blackboard")]
	public class GetField : ActionTask {

		[SerializeField]
		protected System.Type targetType;
		[SerializeField]
		protected string fieldName;
		[SerializeField] [BlackboardOnly]
		protected BBObjectParameter saveAs;

		private FieldInfo field;

		public override System.Type agentType{
			get {return targetType != null? targetType : typeof(Transform);}
		}

		protected override string info{
			get
			{
				if (string.IsNullOrEmpty(fieldName))
					return "No Field Selected";
				return string.Format("{0} = {1}.{2}", saveAs.ToString(), agentInfo, fieldName);
			}
		}

		protected override string OnInit(){
			field = agentType.RTGetField(fieldName);
			if (field == null)
				return "Missing Field: " + fieldName;
			return null;
		}

		protected override void OnExecute(){
			saveAs.value = field.GetValue(agent);
			EndAction();
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override void OnTaskInspectorGUI(){

			if (!Application.isPlaying && GUILayout.Button("Select Field")){
				
				System.Action<FieldInfo> FieldSelected = (field)=> {
					targetType = field.DeclaringType;
					fieldName = field.Name;
					saveAs.SetType(field.FieldType);
				};

				var menu = new UnityEditor.GenericMenu();
				if (agent != null){
					foreach(var comp in agent.GetComponents(typeof(Component)).Where(c => c.hideFlags == 0) ){
						menu = EditorUtils.GetFieldSelectionMenu(comp.GetType(), typeof(object), FieldSelected, menu);
					}
					menu.AddSeparator("/");
				}
				foreach (var t in UserTypePrefs.GetPreferedTypesList(typeof(Component), true)){
					menu = EditorUtils.GetFieldSelectionMenu(t, typeof(object), FieldSelected, menu);
				}

				if ( NodeCanvas.Editor.NCPrefs.useBrowser){ menu.ShowAsBrowser("Select Field", this.GetType()); }
				else { menu.ShowAsContext(); }
				Event.current.Use();
			}


			if (agentType != null && !string.IsNullOrEmpty(fieldName)){
				GUILayout.BeginVertical("box");
				UnityEditor.EditorGUILayout.LabelField("Type", agentType.Name);
				UnityEditor.EditorGUILayout.LabelField("Field", fieldName);
				UnityEditor.EditorGUILayout.LabelField("Field Type", saveAs.varType.FriendlyName() );
				GUILayout.EndVertical();
				EditorUtils.BBParameterField("Save As", saveAs, true);
			}
		}

		#endif
	}
}
