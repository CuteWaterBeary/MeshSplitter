using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class MeshSplitterEditor : EditorWindow
{
	private SkinnedMeshRenderer _mesh_renderer;
	private MeshFilter _mesh_filter;

	[MenuItem ("MeshSplitter/MeshSplitter Editor")]
	private static void Create ()
	{
		GetWindow<MeshSplitterEditor> ("MeshSplitter");
	}

	private void OnGUI ()
	{
		EditorGUI.BeginChangeCheck ();
		_mesh_renderer = EditorGUILayout.ObjectField ("Mesh", _mesh_renderer, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
		EditorGUILayout.Space ();

		_mesh_filter = EditorGUILayout.ObjectField ("Quad", _mesh_filter, typeof(MeshFilter), true) as MeshFilter;
		EditorGUILayout.Space ();

		if (EditorGUI.EndChangeCheck ()) {
		}

		if (_mesh_filter != null && _mesh_filter.sharedMesh.name != "Quad") {
			_mesh_filter = null;
		}

		if (_mesh_renderer != null) {
			if (GUILayout.Button ("split by materials")) {
				SplitByMaterials (_mesh_renderer);
			}
			EditorGUILayout.Space ();

			if (_mesh_filter != null) {
				if (GUILayout.Button ("split by quad")) {
					splitByQuad (_mesh_renderer, _mesh_filter);
				}
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

	private void splitByQuad (SkinnedMeshRenderer mesh_renderer, MeshFilter mesh_filter)
	{
		var plane = createPlane (mesh_filter);
		var mesh = mesh_renderer.sharedMesh;
		var matrix = mesh_renderer.transform.localToWorldMatrix;

		string mesh_name = mesh_renderer.gameObject.name;
		string submesh_dir = getSubmeshPath (mesh_renderer);
		createFolder (submesh_dir);

		var tri_a = new List<List<int>> ();
		var tri_b = new List<List<int>> ();

		for (int j = 0; j < mesh.subMeshCount; j++) {
			var triangles = mesh.GetTriangles (j);
			tri_a.Add (new List<int> ());
			tri_b.Add (new List<int> ());

			for (int i = 0; i < triangles.Length; i += 3) {
				var triangle = triangles.Skip (i).Take (3);
				bool side = false;

				foreach (int n in triangle) {
					side = side || plane.GetSide (matrix.MultiplyPoint (mesh.vertices [n]));
				}

				if (side) {
					tri_a [j].AddRange (triangle);
				} else {
					tri_b [j].AddRange (triangle);
				}

				if (i % 30 == 0) {
					EditorUtility.DisplayProgressBar (
						"処理中", 
						string.Format ("submesh:{0}/{1}, triangles:{2}/{3}", j, mesh.subMeshCount, i, triangles.Length),
						(float)i / triangles.Length
					);
				}
			}
		}

		createNewMesh (mesh_renderer, tri_a.Select (n => n.ToArray ()).ToArray (), submesh_dir, mesh_name + "_a");
		createNewMesh (mesh_renderer, tri_b.Select (n => n.ToArray ()).ToArray (), submesh_dir, mesh_name + "_b");
		mesh_renderer.gameObject.SetActive (false);

		EditorUtility.ClearProgressBar (); 
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
		AssetDatabase.CreateAsset (mesh, Path.Combine (dirname, name + ".asset"));
		mesh_renderer.sharedMesh = mesh;

		return mesh_renderer;
	}

	private Plane createPlane (MeshFilter mesh_filter)
	{
		var matrix = mesh_filter.transform.localToWorldMatrix;
		var mesh = mesh_filter.sharedMesh;
		var vertices = mesh.triangles.Take (3).Select (n => matrix.MultiplyPoint (mesh.vertices [n])).ToArray ();
		return new Plane (vertices [0], vertices [1], vertices [2]);
	}
}
