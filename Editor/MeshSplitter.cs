using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class MeshSplitterEditor : EditorWindow
{
	private SkinnedMeshRenderer target_mesh;

	[MenuItem("MeshSplitter/MeshSplitter Editor")]
	private static void Create() {
		GetWindow<MeshSplitterEditor>("MeshSplitter");
	}

	private void OnGUI() {
		EditorGUI.BeginChangeCheck();
		target_mesh = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Mesh", target_mesh, typeof(SkinnedMeshRenderer), true);

		if (EditorGUI.EndChangeCheck ()) {
		}

		EditorGUILayout.Space();

		if (target_mesh != null) {
			using (new GUILayout.HorizontalScope()) {
				if (GUILayout.Button("split by materials")) {
					SplitByMaterials(target_mesh);
				}
			}
		}
	}

	private void SplitByMaterials(SkinnedMeshRenderer mesh) {
		string submesh_dir = createFolder(getPath(target_mesh.gameObject) + "/Submeshes/" + target_mesh.gameObject.name);

		for (int i=0; i<mesh.sharedMesh.subMeshCount; i++) {
			cloneSubmesh(target_mesh, i, submesh_dir);
		}

		target_mesh.gameObject.SetActive(false);
	}

	private string getPath (GameObject gameObject) {
		Object prefab = PrefabUtility.GetPrefabParent (target_mesh.gameObject);
		string prefab_path = AssetDatabase.GetAssetPath (prefab);
		return Path.GetDirectoryName (prefab_path);
	}

	private string createFolder(string path) {
		if (AssetDatabase.IsValidFolder(path)) {
			return path;
		}

		string parent = Path.GetDirectoryName (path);
		string dirname = Path.GetFileName (path);

		if (!AssetDatabase.IsValidFolder (parent)) {
			Debug.Log (parent);
			createFolder (parent);
		}

		string guid = AssetDatabase.CreateFolder (parent, dirname);
		return AssetDatabase.GUIDToAssetPath (guid);
	}

	private string getMaterialName(SkinnedMeshRenderer mesh, int index) {
		return mesh.sharedMaterials [index].name;
	}

	private void cloneSubmesh(SkinnedMeshRenderer target, int index, string submesh_dir) {
		string material_name = getMaterialName (target, index);
	
		GameObject new_object = Instantiate (target.gameObject, target.gameObject.transform.parent) as GameObject;
		SkinnedMeshRenderer skinned_mesh_renderer = new_object.GetComponent (typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;
		Mesh mesh = Instantiate (skinned_mesh_renderer.sharedMesh) as Mesh;
		mesh.triangles = target_mesh.sharedMesh.GetTriangles (index);
	
		//バウンディングボリュームと法線の再計算
		mesh.RecalculateBounds ();
		mesh.RecalculateNormals ();

		AssetDatabase.CreateAsset (mesh, submesh_dir + "/" + material_name + ".asset");
		skinned_mesh_renderer.sharedMesh = mesh;
		skinned_mesh_renderer.sharedMaterials = new Material[1] { target_mesh.sharedMaterials [index] };
		new_object.name = material_name;
	}
}
