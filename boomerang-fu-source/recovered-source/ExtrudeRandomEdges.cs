using System.Collections.Generic;
using System.Linq;
using ProBuilder2.Common;
using ProBuilder2.MeshOperations;
using UnityEngine;

public class ExtrudeRandomEdges : MonoBehaviour
{
	private pb_Object pb;

	private pb_Face lastExtrudedFace;

	public float distance = 1f;

	private void Start()
	{
		pb = pb_ShapeGenerator.PlaneGenerator(1f, 1f, 0, 0, Axis.Up, smooth: false);
		pb.SetFaceMaterial(pb.faces, pb_Constant.DefaultMaterial);
		lastExtrudedFace = pb.faces[0];
	}

	private void OnGUI()
	{
		if (GUILayout.Button("Extrude Random Edge"))
		{
			ExtrudeEdge();
		}
	}

	private void ExtrudeEdge()
	{
		pb_Face sourceFace = lastExtrudedFace;
		List<pb_Edge> list = (from y in pb_WingedEdge.GetWingedEdges(pb)
			where y.face == sourceFace
			where y.opposite == null
			select y.edge.local).ToList();
		int index = Random.Range(0, list.Count);
		pb_Edge pb_Edge2 = list[index];
		Vector3 vector = (pb.vertices[pb_Edge2.x] + pb.vertices[pb_Edge2.y]) * 0.5f - sourceFace.distinctIndices.Average((int x) => pb.vertices[x]);
		vector.Normalize();
		pb.Extrude(new pb_Edge[1] { pb_Edge2 }, 0f, extrudeAsGroup: false, enableManifoldExtrude: true, out var extrudedEdges);
		lastExtrudedFace = pb.faces.Last();
		pb.SetSelectedEdges(extrudedEdges);
		pb.TranslateVertices(pb.SelectedTriangles, vector * distance);
		pb.ToMesh();
		pb.Refresh();
	}
}
