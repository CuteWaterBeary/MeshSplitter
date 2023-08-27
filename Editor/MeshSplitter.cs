using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class MeshSplitterEditor : EditorWindow
{
	private SkinnedMeshRenderer _mesh_renderer;

	[MenuItem ("Mesh+Bones/Split/Split Mesh by Materials")]
	private static void Create ()
	{
		GetWindow<MeshSplitterEditor> ("MeshSplitter");
	}

	private void OnGUI ()
	{
		EditorGUI.BeginChangeCheck ();
		_mesh_renderer = EditorGUILayout.ObjectField ("Mesh", _mesh_renderer, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
		EditorGUILayout.Space ();

		if (EditorGUI.EndChangeCheck ()) {
		}

		if (_mesh_renderer != null) {
			if (GUILayout.Button ("Split by materials")) {
				SplitByMaterials (_mesh_renderer);
			}
		}

	}

	private void SplitByMaterials (SkinnedMeshRenderer mesh_renderer)
	{
		string submesh_dir = getSubmeshPath (mesh_renderer);
		createFolder (submesh_dir);

		for (int i = 0; i < mesh_renderer.sharedMesh.subMeshCount; i++) {
			splitSubmesh (mesh_renderer, i, submesh_dir);
		}

		mesh_renderer.gameObject.SetActive (false);
	}

	private string getSubmeshPath (SkinnedMeshRenderer mesh_renderer)
	{
		string mesh_path = AssetDatabase.GetAssetPath (mesh_renderer.sharedMesh);
		string base_dir = Path.GetDirectoryName (mesh_path);

		if (base_dir.EndsWith ("Submeshes")) {
			return base_dir;
		} else {
			return Path.Combine (base_dir, "Submeshes");
		}
	}

	private void createFolder (string path)
	{
		if (AssetDatabase.IsValidFolder (path)) {
			return;
		}

		string parent = Path.GetDirectoryName (path);
		string dirname = Path.GetFileName (path);

		if (!AssetDatabase.IsValidFolder (parent)) {
			createFolder (parent);
		}

		AssetDatabase.CreateFolder (parent, dirname);
	}

	private void splitSubmesh (SkinnedMeshRenderer mesh_renderer, int index, string submesh_dir)
	{
		string material_name = mesh_renderer.sharedMaterials [index].name;
		string mesh_name = mesh_renderer.gameObject.name + "_" + material_name;
		var triangles = new int[][] { mesh_renderer.sharedMesh.GetTriangles (index) };

		var new_mesh_renderer = createNewMesh (mesh_renderer, triangles, submesh_dir, mesh_name);
		new_mesh_renderer.sharedMaterials = new Material[] { mesh_renderer.sharedMaterials [index] };
	}

	private GameObject cloneObject (GameObject gameObject)
	{
		return Instantiate (gameObject, gameObject.transform.parent) as GameObject;
	}


	private SkinnedMeshRenderer createNewMesh (SkinnedMeshRenderer original, int[][] triangles, string dirname, string name)
	{
		var gameObject = cloneObject (original.gameObject);
		gameObject.name = name;
		var mesh_renderer = gameObject.GetComponent (typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;
		var mesh = Instantiate (mesh_renderer.sharedMesh) as Mesh;

		mesh.subMeshCount = triangles.Length;
		for (int i = 0; i < triangles.Length; i++) {
			mesh.SetTriangles (triangles [i], i);
		}
		AssetDatabase.CreateAsset (mesh, Path.Combine (dirname, name + ".mesh"));
		mesh_renderer.sharedMesh = mesh;

		return mesh_renderer;
	}
}
