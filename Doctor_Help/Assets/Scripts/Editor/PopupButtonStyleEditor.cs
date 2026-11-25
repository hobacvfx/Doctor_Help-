using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace DoctorHelp.Editor {
 public static class PopupButtonStyleEditor {
  const string TexturePath="Assets/2D_Assets/DartMinigame/Button_Confirm.png";
  const string Report="Design/UI/SharedPopupButtons/Installation.txt";
  static readonly Color Cream=new Color(1,.94f,.8f);
  static Sprite frame; static Material red; static int changed;
  [MenuItem("Tools/Doctor Help/UI/Apply Shared Popup Button Frame")]
  public static void Apply(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
   for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene edits first.");
   Directory.CreateDirectory("Design/UI/SharedPopupButtons");File.WriteAllText(Report,"Shared popup button installation\n");changed=0;
   Import();
   var shader=Shader.Find("DoctorHelp/UI/Button Accent");if(shader==null)throw new Exception("Button Accent shader not imported");
   const string materialPath="Assets/Art/UI/SharedButtons/PopupButtonRed.mat";red=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
   if(red==null){red=new Material(shader);AssetDatabase.CreateAsset(red,materialPath);}red.SetColor("_Accent",new Color(.52f,.10f,.12f));EditorUtility.SetDirty(red);
   BuildReusable();
   var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources/UI","Assets/Prefabs/Basement/UI"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>!p.EndsWith("PopupActionButton.prefab")).ToList();
   // Process dependencies before containers, preserving nested prefab links.
   var ordered=new List<string>();var seen=new HashSet<string>();foreach(var path in paths)Order(path,paths,seen,ordered);
   foreach(var path in ordered){var root=PrefabUtility.LoadPrefabContents(path);try{int before=changed;foreach(var b in root.GetComponentsInChildren<Button>(true))if(IsAction(b,root.transform,path))Style(b,path);if(before!=changed)PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
   foreach(var path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath)){
    var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
    try{int before=changed;foreach(var root in scene.GetRootGameObjects())foreach(var b in root.GetComponentsInChildren<Button>(true))if(IsAction(b,root.transform,path))Style(b,path);if(changed!=before)EditorSceneManager.SaveScene(scene);}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
   }
   AssetDatabase.SaveAssets();File.AppendAllText(Report,"DONE: "+changed+" button appearances updated; shared source sprite border "+frame.border+"; texture pixels unchanged.\n");
  }
  static void Order(string p,List<string> all,HashSet<string> seen,List<string> output){if(!seen.Add(p))return;foreach(var dep in AssetDatabase.GetDependencies(p,false))if(dep!=p&&all.Contains(dep))Order(dep,all,seen,output);output.Add(p);}
  static void Import(){var importer=(TextureImporter)AssetImporter.GetAtPath(TexturePath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.spritePixelsPerUnit=100;var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
#pragma warning disable 618
   importer.spritesheet=new[]{new SpriteMetaData{name="Button_Confirm",rect=new Rect(7,29,498,113),alignment=0,pivot=Vector2.one*.5f,border=new Vector4(60,56,60,56)}};
#pragma warning restore 618
   foreach(var platform in new[]{"Android","iPhone"}){var s=importer.GetPlatformTextureSettings(platform);s.name=platform;s.overridden=true;s.maxTextureSize=2048;s.format=TextureImporterFormat.ASTC_4x4;s.textureCompression=TextureImporterCompression.Compressed;importer.SetPlatformTextureSettings(s);}importer.SaveAndReimport();frame=AssetDatabase.LoadAllAssetsAtPath(TexturePath).OfType<Sprite>().Single();
  }
  static bool IsAction(Button b,Transform root,string path){
   if(b.GetComponentsInParent<MonoBehaviour>(true).Any(c=>c!=null && (c.GetType().Name=="GivenMedicineSlot" || c.GetType().Name=="TreatmentMedicineRow" || c.GetType().Name=="PillOptionButton")))return false;
   string n=b.name.ToLowerInvariant();if(n=="outside"||n=="panel"||n=="openingcomic"||n=="inventorypopup"||n=="selectpillpopup"||n=="shoppopup"||n=="policereportpopup")return false;
   if(n=="rewarded ad darts"||n=="paid spin"||n=="rewarded ad spin")return true;
   if(path.EndsWith("/MedicineRow.prefab")||path.EndsWith("InventoryMedicineRow.prefab")||path.EndsWith("ClinicShopRow.prefab")||path.EndsWith("BasementPlantCard.prefab")||path.EndsWith("TreatmentDoneButton.prefab"))return true;
   for(var t=b.transform;t!=null;t=t.parent){string p=t.name.ToLowerInvariant();if(p.Contains("popup")||p.Contains("confirmation")||p=="clinicnaming"||p=="walterdialogue"||p=="tutorialoverlay"||p=="clinicinventory"||p.EndsWith("upgradeshop")||p=="streetconsumablesshop"||(p=="panel"&&path.EndsWith("AdDiagnostics.prefab")))return true;}
   return false;
  }
  static void Style(Button b,string path){
   var image=b.targetGraphic as Image;if(image==null)return;
   var texts=b.GetComponentsInChildren<Text>(true).Where(t=>t.GetComponentInParent<Button>(true)==b).ToArray();
   string n=b.name.ToLowerInvariant();bool danger=n=="no"||n.Contains("reset")||n.Contains("decline");var material=danger?red:null;
   bool needs=image.sprite!=frame||image.type!=Image.Type.Sliced||image.material!=(material==null?Graphic.defaultGraphicMaterial:material)||image.color!=Color.white||image.preserveAspect||!image.fillCenter;
   if(!needs && image.GetComponent<PopupButtonFrame>()!=null)return;
   image.sprite=frame;image.overrideSprite=null;image.type=Image.Type.Sliced;image.fillCenter=true;image.preserveAspect=false;image.color=Color.white;image.material=material;
   if(image.GetComponent<PopupButtonFrame>()==null)image.gameObject.AddComponent<PopupButtonFrame>();
   // 540-unit HUDs use ~2 source pixels per UI unit; 1080-unit popups use 1.
   var scaler=b.GetComponentInParent<CanvasScaler>();float reference=scaler!=null?scaler.referenceResolution.x:540;image.pixelsPerUnitMultiplier=reference>=1000?1:2;
   b.transition=Selectable.Transition.ColorTint;var colors=b.colors;colors.normalColor=colors.selectedColor=Color.white;colors.highlightedColor=new Color(1,1,1,1);colors.pressedColor=new Color(.78f,.78f,.78f,1);colors.disabledColor=new Color(.5f,.5f,.5f,.65f);b.colors=colors;b.spriteState=default;
   foreach(var t in texts){t.color=Cream;Record(t);}
   if(texts.Length==0&&(n=="close"||n=="exit"||n=="add"||n=="swap")){
    var go=new GameObject("Symbol",typeof(RectTransform),typeof(Text));go.layer=5;var r=(RectTransform)go.transform;r.SetParent(b.transform,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
    var label=go.GetComponent<Text>();label.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/UI/Newspaper/Fonts/OldStandardTT-Bold.ttf");label.text=n=="add"?"+":n=="swap"?"↔":"×";label.fontSize=n=="add"?62:28;label.alignment=TextAnchor.MiddleCenter;label.color=Cream;label.raycastTarget=false;
   }
   var state=b.GetComponent<BasementButtonState>();if(state!=null){var s=new SerializedObject(state);s.FindProperty("enabledSprite").objectReferenceValue=frame;s.FindProperty("disabledSprite").objectReferenceValue=frame;s.ApplyModifiedPropertiesWithoutUndo();Record(state);}
   Record(image);Record(b);changed++;File.AppendAllText(Report,path+" | "+Hierarchy(b.transform)+"\n");
  }
  static void Record(UnityEngine.Object o){EditorUtility.SetDirty(o);if(PrefabUtility.IsPartOfPrefabInstance(o))PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
  static string Hierarchy(Transform t)=>t.parent==null?t.name:Hierarchy(t.parent)+"/"+t.name;
  static void BuildReusable(){var go=new GameObject("PopupActionButton",typeof(RectTransform),typeof(Image),typeof(Button));go.layer=5;var r=(RectTransform)go.transform;r.sizeDelta=new Vector2(164,58);var i=go.GetComponent<Image>();i.sprite=frame;i.type=Image.Type.Sliced;i.pixelsPerUnitMultiplier=2;go.AddComponent<PopupButtonFrame>();var b=go.GetComponent<Button>();b.targetGraphic=i;b.navigation=new Navigation{mode=Navigation.Mode.None};PrefabUtility.SaveAsPrefabAsset(go,"Assets/Resources/UI/PopupActionButton.prefab");UnityEngine.Object.DestroyImmediate(go);}
 }
}
