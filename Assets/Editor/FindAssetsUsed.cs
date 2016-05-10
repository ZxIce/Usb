#define UseAutoFind
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Reflection;
public class ArtFindSet{
	public bool _onlyShowNoUsed;
}
public class ArtAssetsEx : EditorWindow{
	#region Init
	static ArtAssetsEx Instance;
	static List<string> _filePrefabPath = new List<string>();
	static List<string> _fileAssetsPath = new List<string>();
	static List<PrefabDependencies> _prefabs = new List<PrefabDependencies>();
	
	static List<string> _readyToFind;
	static bool _path;
	static public AssetUsedData _AssetUsedData;
	static public AllAssetsData _AllAssetsData;
	static public string[] _AssetType;
	static public int _SelectAssetType = -1;
	static public ArtFindSet _artFindSet = new ArtFindSet();
	static bool _repaint;
	[MenuItem("Window/资源浏览器")]
	public static void Init(){
		Instance = EditorWindow.GetWindow<ArtAssetsEx> ();
//#if UseAutoFind
		_filePrefabPath.Add (Application.dataPath);
//		Find ();
//#endif
	}
//	[MenuItem("Assets/设为Find Art Prefab Path")]
//	public static void AddFilePrefabPath(){
//		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
//		path = path.Replace ("Assets", "");
//		_filePrefabPath.Add(Application.dataPath + path);
//	}
//	[MenuItem("Assets/设为Find Art Assets Path")]
//	public static void AddFileAssetsPath(){
//		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
//		path = path.Replace ("Assets", "");
//		_fileAssetsPath.Add(Application.dataPath + path);
//	}
	[MenuItem("Assets/查找这个东西使用的情况")]
	public static void FindUsePrefab(){
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
		if (Instance == null) {
			Init();
			Find();
		}
		_AssetUsedData = new AssetUsedData (path);
		_AssetUsedData.FindUsed (_prefabs);
		_repaint = true;
	}
//	[MenuItem("Assets/设为Find Use Prefab")]
//	public static void FindUsePrefab(){
//		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
////		AssetDatabase.SetLabels(
//	}
	static public string[] _toolStr = {"浏览器","设置"};
	static public int _selectTool;
	void OnGUI(){
		_selectTool = GUILayout.Toolbar (_selectTool, _toolStr);
		switch (_selectTool) {
		case 1:
			GUILayout.Label ("预制路径");
			for (int i = 0; i < _filePrefabPath.Count; i++) {
				EditorGUILayout.BeginHorizontal ();
				if (GUILayout.Button ("-", GUILayout.Width (30))) {
					_filePrefabPath.RemoveAt (i);
					i++;
				} else {
					GUILayout.Label (_filePrefabPath [i]);
				}
				EditorGUILayout.EndHorizontal ();
			}
			GUILayout.Label ("资源路径");
			for (int i = 0; i < _fileAssetsPath.Count; i++) {
				EditorGUILayout.BeginHorizontal ();
				if (GUILayout.Button ("-", GUILayout.Width (30))) {
					_fileAssetsPath.RemoveAt (i);
					i++;
				} else {
					GUILayout.Label (_fileAssetsPath [i]);
				}
				EditorGUILayout.EndHorizontal ();
			}
			break;
		case 0:
//				if (GUILayout.Button ("Find",GUILayout.Height(100),GUILayout.MaxWidth(300)))
//					Find ();
				if(_prefabs != null && _AllAssetsData!=null){
				EditorGUILayout.BeginVertical();
					EditorGUILayout.LabelField("项目的预制与场景数目:"+_prefabs.Count);
					EditorGUILayout.LabelField("项目使用的文件类型数:"+_AllAssetsData._data.Count);
				EditorGUILayout.EndVertical();

				}
				ShowTest ();	
			break;
		}
		if (_repaint) {
		Repaint ();
		}
	}
	Vector2 pos;
	void ShowTest(){
		bool change = false;
		EditorGUILayout.BeginHorizontal ();
			if (_AssetType != null) {
				_SelectAssetType = EditorGUILayout.Popup("查询文件类型:",_SelectAssetType,_AssetType,GUILayout.MaxWidth(300));
				if(GUI.changed){
					change = true;
					EditorGUILayout.EndHorizontal ();
					_artFindSet._onlyShowNoUsed = false;
					_AllAssetsData.GetDepend(_SelectAssetType,_prefabs);
				}
			}
		if(!change)
			EditorGUILayout.EndHorizontal ();
		if (_AssetUsedData != null) {
			if(GUILayout.Button("查看全局数据"))
				_AssetUsedData = null;
			if(_AssetUsedData != null)
				_AssetUsedData.OnGUI ();
		}
		if(_AssetUsedData == null && _SelectAssetType != -1){
			_artFindSet._onlyShowNoUsed = EditorGUILayout.Toggle(!_artFindSet._onlyShowNoUsed?"显示使用的":"显示未使用的" ,_artFindSet._onlyShowNoUsed);
			EditorGUILayout.LabelField(_AssetType[_SelectAssetType]+"总资源数:"+_AllAssetsData._data[_SelectAssetType]._allAssets);
			EditorGUILayout.LabelField(_AssetType[_SelectAssetType]+"被使用数:"+_AllAssetsData._data[_SelectAssetType]._allUsedAssets);
			EditorGUILayout.LabelField(_AssetType[_SelectAssetType]+"未使用数:"+_AllAssetsData._data[_SelectAssetType]._allUnUsedAssets);
			pos = EditorGUILayout.BeginScrollView (pos);
			_AllAssetsData.OnGUI(_SelectAssetType,_artFindSet);
			EditorGUILayout.EndScrollView ();
		}
	}
	#endregion
	#region Find
	static void Find(){
		_SelectAssetType = -1;
		if (_filePrefabPath == null || _filePrefabPath.Count == 0)
			_filePrefabPath.Add (Application.dataPath);
		_readyToFind = new List<string> ();
		_prefabs = new List<PrefabDependencies> ();
		_AllAssetsData = new AllAssetsData ();
		for (int i = 0; i < _filePrefabPath.Count; i++) {
			FindAllPath(_filePrefabPath[i]);
		}

		CreatePrefabData ();
		RestPrefabDependencie ();

		_AssetType = new string[_AllAssetsData._data.Count];
		int count = 0;
		for (int i = 0;i < _AllAssetsData._data.Count;i++) {
			_AssetType[count] = _AllAssetsData._data[i]._expandname;
			count++;
		}

	}
	static void CreatePrefabData(){
		for(int i = 0;i < _readyToFind.Count;i++){
			string expandname = Path.GetExtension(_readyToFind[i]);
			if(expandname == ".prefab"||expandname == ".unity"){
				PrefabDependencies pd = new PrefabDependencies(_readyToFind[i]);
				_prefabs.Add(pd);
			}
			_AllAssetsData.AddAssets(_readyToFind[i]);
		}
	}
	static void RestPrefabDependencie(){
		for (int i = 0; i < _prefabs.Count; i++) {
			_prefabs[i].GetDependencies();
			if(EditorUtility.DisplayCancelableProgressBar("获取索引","GetDependencie:"+i,(float)i/_prefabs.Count)){
				EditorUtility.ClearProgressBar ();
				return;
			}
		}
		EditorUtility.ClearProgressBar ();
	}
	static void FindAllPath(string path){
		string[] Directorys = new string[0]; 
		try{
			Directorys = Directory.GetFiles(path);
		}catch{}
		if(Directorys != null){
			for(var i = 0;i < Directorys.Length;i++){
				if(!_readyToFind.Contains(Directorys[i]))
				_readyToFind.Add(Directorys[i]);
			}
		}
		Directorys = Directory.GetDirectories (path);
		for (int i = 0; i < Directorys.Length; i++) {
			string newpath;
			newpath = Path.GetDirectoryName(Directorys[i])+"/"+Path.GetFileName(Directorys[i]);
			FindAllPath(newpath);
		}
	}
	#endregion
}
public class AllAssetsData{

	public List<AssetsUsedData> _data = new List<AssetsUsedData>();
	public void GetDepend(int _id,List<PrefabDependencies> prefabs){
		if(_id>=0&&_id<_data.Count)
		_data [_id].GetDepend (prefabs);
	}
	public void AddAssets(string path){
		string expandname = Path.GetExtension(path);
		if (expandname == ".meta")
			return;
		int id = IsContainsKey (expandname);
		if (id == -1) {
			_data.Add(new AssetsUsedData(expandname));
			id = _data.Count - 1;
		}
		_data[id].AddData(path);
	}
	public int IsContainsKey(string expandname){
		for(int i = 0;i < _data.Count;i++){
			if(_data[i]._expandname == expandname)
				return i;
		}
		return -1;
	}
	public void OnGUI(int _SelectAssetType,ArtFindSet set){
		if (_SelectAssetType >= 0 && _SelectAssetType < _data.Count) {
			int x = 0;
			for(int i = 0;i < _data[_SelectAssetType]._data.Count;i++){
				if((_data[_SelectAssetType]._data[i]._usedPrefab.Count == 0 && set._onlyShowNoUsed)||
				   (_data[_SelectAssetType]._data[i]._usedPrefab.Count > 0 && !set._onlyShowNoUsed)
				   ){
					Color c = (x%2==0?new Color(0.8f,0.8f,0.8f):Color.white);
					GUI.color = c;
					EditorGUILayout.BeginVertical("box");
					GUI.color = Color.white;
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Used:"+_data[_SelectAssetType]._data[i]._usedPrefab.Count+"  ",GUILayout.Width(100));
//					EditorGUILayout.LabelField(_data[_SelectAssetType]._data[i]._path);
//					EditorGUILayout.ObjectField(_data[_SelectAssetType]._data[i].assetObj,typeof(GameObject));
					EditorGUILayout.EndHorizontal();
					_data[_SelectAssetType]._data[i].OnGUI();
					EditorGUILayout.EndVertical();
					x++;
				}
			}
		}
//		foreach (KeyValuePair<string,AssetsUsedData> value in _data) {
//			List<AssetUsedData> datas = value.Value._data;
//			for(int i= 0;i < datas.Count;i++){
//				EditorGUILayout.LabelField(datas[i]._path);
//				datas[i].OnGUI();
//			}
//		}
	}
}
public class AssetsUsedData{
	public int _allAssets;
	public int _allUsedAssets;
	public int _allUnUsedAssets;
	public string _expandname;
	public AssetsUsedData(string expandname){
		_expandname = expandname;
	}
	public List<AssetUsedData> _data = new List<AssetUsedData>();
	public void AddData(string path){
		path = "Assets"+path.Replace(Application.dataPath,"");
		path = path.Replace ("\\", "/");
		_data.Add (new AssetUsedData (path));
	}
	public void GetDepend(List<PrefabDependencies> prefabs){
		for(int i = 0;i<_data.Count;i++){
			if(EditorUtility.DisplayCancelableProgressBar("搜索使用情况","数量:"+i,(float)i/_data.Count)){
				_data.Clear();
				EditorUtility.ClearProgressBar ();
				return;
			}
			_data[i].FindUsed(prefabs);

		}
		EditorUtility.ClearProgressBar ();
		for (int i = 0; i < _data.Count; i++) {
			if(EditorUtility.DisplayCancelableProgressBar("加载模型","数量:"+i,(float)i/_data.Count)){
				_data.Clear();
				EditorUtility.ClearProgressBar ();
				return;
			}
			_data[i].LoadAssetObj();
		}
		EditorUtility.ClearProgressBar ();
		_allAssets = _data.Count;
		_allUsedAssets = 0;
		for (int i = 0; i < _data.Count; i++) {
			if(EditorUtility.DisplayCancelableProgressBar("统计数据","数量:"+i,(float)i/_data.Count)){
				_data.Clear();
				EditorUtility.ClearProgressBar ();
				return;
			}
			if(_data[i]._usedPrefab.Count > 0){
				_allUsedAssets ++;
			}
		}
		_allUnUsedAssets = _allAssets - _allUsedAssets;
		EditorUtility.ClearProgressBar ();
	}
}
public class AssetUsedData{
	public AssetUsedData(string path){
		_path = path;
	}
	public string _path;
	public Object _assetObj;
	public void LoadAssetObj(){
		if (_assetObj == null)
			_assetObj = AssetDatabase.LoadAssetAtPath(_path,typeof(Object));
	}
	public Object assetObj{
		get{
			if (_assetObj == null)
				_assetObj = AssetDatabase.LoadAssetAtPath(_path,typeof(Object));
			return _assetObj;
		}
	}
	public List<Object> _usedPrefab = new List<Object>();
	public void FindUsed(List<PrefabDependencies> prefabs){
		_usedPrefab = new List<Object> ();
		for (int i = 0; i < prefabs.Count; i++) {
			if(prefabs[i]._dependencies.Contains(_path))
			{
				_usedPrefab.Add (AssetDatabase.LoadAssetAtPath(prefabs[i]._prefabPath,typeof(Object)));
			}
		}
	}
	public void OnGUI(){
		EditorGUILayout.BeginVertical ();
		EditorGUILayout.ObjectField("查询资源:",assetObj,typeof(GameObject));
		for (int i = 0; i < _usedPrefab.Count; i++) {
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.ObjectField("使用的预制:",_usedPrefab[i],typeof(GameObject));
//			if(GUILayout.Button("See"))
//				SeeAssetData.SeeAsset(_usedPrefab[i],_path);
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndVertical ();
	}
}
public class PrefabDependencies{
	public PrefabDependencies(string path){
		_prefabPath = "Assets"+path.Replace(Application.dataPath,"");
		_prefabPath = _prefabPath.Replace ("\\", "/");
	}
	//.prefab or unity
	public string _prefabPath;
	public List<string> _dependencies;
	public bool _showDependencies;
	public void GetDependencies(){
		string[] path = new string[1];
		path [0] = _prefabPath;
		string[] paths =AssetDatabase.GetDependencies (path); 
		_dependencies = new List<string> ();
		for(int i = 0;i < paths.Length;i++){
			_dependencies.Add(paths[i]);
		}
	}
}
public class SeeAssetData{
	static Transform _parent;
	public static Transform Parent{
		get{
			if (_parent == null)
				_parent = new GameObject ("Find Assets See").transform;
			return _parent;
		}
	}
	public static void SeeAsset(Object prefab,string asset){
		string path = AssetDatabase.GetAssetPath (prefab);
		string _expandname = Path.GetExtension (path);
		Debug.Log (_expandname);
		switch (_expandname) {
		case ".prefab":
			SeeAssetsInPrefab(prefab,asset);
				break;
		case ".unity":
			SeeAssetsInScene(prefab,asset);
			break;
		}
	}

	static void SeeAssetsInPrefab(Object prefab,string asset){
		GameObject obj = GameObject.Instantiate (prefab) as GameObject;
		obj.transform.parent = Parent;
		HierarchyProperty h = new HierarchyProperty(HierarchyType.Assets);
		h.SetSearchFilter ("x",0);

	}
	static void SeeAssetsInScene(Object prefab,string asset){

	}
	[MenuItem("Window/test")]
	static void Test(){
//		Assembly m = typeof(EditorWindow).Assembly;
//		Type t = m.GetType ("UnityEditor.DockArea");
//		UnityEngine.Object d = System.Activator.CreateInstance (t);
//		HierarchyProperty h;
//		h.SetSearchFilter ("x",0);
	}
}
