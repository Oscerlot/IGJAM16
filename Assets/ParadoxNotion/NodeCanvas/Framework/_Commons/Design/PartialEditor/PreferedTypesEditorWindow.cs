﻿#if UNITY_EDITOR

using System.Collections.Generic;
using ParadoxNotion;
using ParadoxNotion.Serialization;
using UnityEditor;
using UnityEngine;


namespace ParadoxNotion.Design{

	public class PreferedTypesEditorWindow : EditorWindow {

		private List<System.Type> typeList;
		private Vector2 scrollPos;
		private string search;

		public static void ShowWindow(){
			var window = GetWindow<PreferedTypesEditorWindow>();
			window.Show();
		}

		void OnEnable(){
	        #if UNITY_5
	        titleContent = new GUIContent("Preferred Types");
	        #else
	        title = "Preferred Types";
	        #endif

			typeList = UserTypePrefs.GetPreferedTypesList(typeof(object), true);
			search = null;
		}

		void OnGUI(){
			
			EditorGUILayout.HelpBox("Here you can specify frequently used types for your game for easier access wherever you need to select a type, like for example when you create a new blackboard variable or using any refelection based actions. Furthermore, it is essential when working with AOT platforms like iOS or WebGL, that you generate an AOT Classes and link.xml files with the relevant button bellow.\nTo add types in the list quicker, you can also Drag&Drop an object, or a Script file in this editor window, or seach it bellow.", MessageType.Info);



			GUILayout.BeginHorizontal();
			search = EditorGUILayout.TextField(search, (GUIStyle)"ToolbarSeachTextField");
			if (GUILayout.Button("", (GUIStyle)"ToolbarSeachCancelButton")){
				search = null;
				GUIUtility.keyboardControl = 0;
			}
			GUILayout.EndHorizontal();

			if (search != null && search.Length > 2){
				GUILayout.Label(string.Format("<b>{0}</b>", "Showing Search Results. Click the plus button to add the type."));
				GUILayout.Space(5);
				scrollPos = GUILayout.BeginScrollView(scrollPos);
				foreach(var type in EditorUtils.GetAssemblyTypes(typeof(object))){
					if (type.Name.ToUpper().Contains( search.ToUpper() ) ){
						GUILayout.BeginHorizontal("box");
						if (GUILayout.Button("+", GUILayout.Width(20))){
							AddType(type);
						}						
						EditorGUILayout.LabelField(type.Name, type.Namespace);
						GUILayout.EndHorizontal();
					}
				}
				GUILayout.EndScrollView();

				return;
			}



			if (GUILayout.Button("Add New Type")){
				GenericMenu.MenuFunction2 Selected = delegate(object o){
					if (o is System.Type){
						AddType( (System.Type)o );
					}
					if (o is string){ //namespace
						foreach(var type in EditorUtils.GetAssemblyTypes(typeof(object))){
							if (type.Namespace == (string)o ){
								AddType(type);
							}
						}
					}
				};	

				var menu = new UnityEditor.GenericMenu();
				menu.AddItem(new GUIContent("Classes/System/Object"), false, Selected, typeof(object));
				foreach(var t in EditorUtils.GetAssemblyTypes(typeof(object))){
					var friendlyName = (string.IsNullOrEmpty(t.Namespace)? "No Namespace/" : t.Namespace.Replace(".", "/") + "/") + t.FriendlyName();
					var category = "Classes/";
					if (t.IsInterface) category = "Interfaces/";
					if (t.IsEnum) category = "Enumerations/";
					menu.AddItem(new GUIContent( category + friendlyName), false, Selected, t);

					if (t.Namespace != null){
						var ns = t.Namespace.Replace(".", "/");
						menu.AddItem( new GUIContent( "Namespaces/" + ns), false, Selected, t.Namespace );
					}

				}
				menu.ShowAsContext();
				Event.current.Use();
			}

#if !UNITY_WEBPLAYER

			if (GUILayout.Button("Generate AOTClasses.cs and link.xml Files")){
				if (EditorUtility.DisplayDialog("Generate AOT Classes", "A script relevant to AOT compatibility for certain platforms will now be generated.", "OK")){
					var path = EditorUtility.SaveFilePanelInProject ("AOT Classes File", "AOTClasses", "cs", "");
		            if (!string.IsNullOrEmpty(path)){
						AOTClassesGenerator.GenerateAOTClasses(path);
		            }
				}

				if (EditorUtility.DisplayDialog("Generate link.xml File", "A file relevant to 'code stripping' for platforms that have code stripping enabled will now be generated.", "OK")){
					var path = EditorUtility.SaveFilePanelInProject ("AOT link.xml", "link", "xml", "");
		            if (!string.IsNullOrEmpty(path)){
						AOTClassesGenerator.GenerateLinkXML(path);
		            }
				}

                AssetDatabase.Refresh();
			}

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("RESET DEFAULTS")){
				if (EditorUtility.DisplayDialog("Reset Preferred Types", "Are you sure?", "Yes", "NO!")){
					UserTypePrefs.ResetTypeConfiguration();
					typeList = UserTypePrefs.GetPreferedTypesList(typeof(object), true);
					Save();
				}
			}

			if (GUILayout.Button("Save Preset")){
				var path = EditorUtility.SaveFilePanelInProject ("Save Types Preset", "", "typePrefs", "");
	            if (!string.IsNullOrEmpty(path)){
	                System.IO.File.WriteAllText( path, JSONSerializer.Serialize(typeof(List<System.Type>), typeList, true) );
	                AssetDatabase.Refresh();
	            }		
			}

			if (GUILayout.Button("Load Preset")){
	            var path = EditorUtility.OpenFilePanel("Load Types Preset", "Assets", "typePrefs");
	            if (!string.IsNullOrEmpty(path)){
	                var json = System.IO.File.ReadAllText(path);
	                typeList = JSONSerializer.Deserialize<List<System.Type>>(json);
	                Save();
	            }	
			}

			GUILayout.EndHorizontal();

#endif

			GUILayout.Space(5);

			scrollPos = GUILayout.BeginScrollView(scrollPos);

			for (int i = 0; i < typeList.Count; i++){
				GUILayout.BeginHorizontal("box");
				EditorGUILayout.LabelField(typeList[i].Name, typeList[i].Namespace);
				if (GUILayout.Button("X", GUILayout.Width(18))){
					RemoveType(typeList[i]);
				}
				GUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();

			AcceptDrops();

			Repaint();
		}


 		//Handles Drag&Drop operations
		void AcceptDrops(){
			var e = Event.current;
			if (e.type == EventType.DragUpdated){
				DragAndDrop.visualMode = DragAndDropVisualMode.Link;
			}

			if (e.type == EventType.DragPerform){
				DragAndDrop.AcceptDrag();

				foreach(var o in DragAndDrop.objectReferences){
					
					if (o == null){
						continue;
					}

					if (o is MonoScript){
						var type = (o as MonoScript).GetClass();
						if (type != null){
							AddType(type);
						}
						continue;
					}

					AddType(o.GetType());
				}
			}
		}

		void AddType(System.Type t){
			if (!typeList.Contains(t)){
				typeList.Add(t);
				Save();
				ShowNotification(new GUIContent(string.Format("Type '{0}' Added!", t) ));
				return;
			}

			ShowNotification(new GUIContent(string.Format("Type '{0}' is already in the list.", t.Name) ) );
		}

		void RemoveType(System.Type t){
			typeList.Remove(t);
			Save();
			ShowNotification(new GUIContent(string.Format("Type '{0}' Removed.", t) ));
		}

		void Save(){
			UserTypePrefs.SetPreferedTypesList(typeList);
		}
	}
}

#endif