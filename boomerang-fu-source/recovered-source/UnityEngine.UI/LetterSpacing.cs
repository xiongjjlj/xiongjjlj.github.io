using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityEngine.UI
{
	[AddComponentMenu("UI/Effects/Letter Spacing", 15)]
	public class LetterSpacing : BaseMeshEffect
	{
		private const string SupportedTagRegexPattersn = "<b>|</b>|<i>|</i>|<size=.*?>|</size>|<color=.*?>|</color>|<material=.*?>|</material>";

		[SerializeField]
		private bool useRichText;

		[SerializeField]
		private float m_spacing;

		public float spacing
		{
			get
			{
				return m_spacing;
			}
			set
			{
				if (m_spacing != value)
				{
					m_spacing = value;
					if (base.graphic != null)
					{
						base.graphic.SetVerticesDirty();
					}
				}
			}
		}

		protected LetterSpacing()
		{
		}

		public override void ModifyMesh(VertexHelper vh)
		{
			if (IsActive())
			{
				List<UIVertex> list = new List<UIVertex>();
				vh.GetUIVertexStream(list);
				ModifyVertices(list);
				vh.Clear();
				vh.AddUIVertexTriangleStream(list);
			}
		}

		public void ModifyVertices(List<UIVertex> verts)
		{
			if (!IsActive())
			{
				return;
			}
			Text component = GetComponent<Text>();
			if (component == null)
			{
				Debug.LogWarning("LetterSpacing: Missing Text component");
				return;
			}
			string[] array = component.text.Split('\n');
			float num = spacing * (float)component.fontSize / 100f;
			float num2 = 0f;
			int num3 = 0;
			bool flag = useRichText && component.supportRichText;
			IEnumerator enumerator = null;
			Match match = null;
			switch (component.alignment)
			{
			case TextAnchor.UpperLeft:
			case TextAnchor.MiddleLeft:
			case TextAnchor.LowerLeft:
				num2 = 0f;
				break;
			case TextAnchor.UpperCenter:
			case TextAnchor.MiddleCenter:
			case TextAnchor.LowerCenter:
				num2 = 0.5f;
				break;
			case TextAnchor.UpperRight:
			case TextAnchor.MiddleRight:
			case TextAnchor.LowerRight:
				num2 = 1f;
				break;
			}
			foreach (string text in array)
			{
				int lineLengthWithoutTags = text.Length;
				if (flag)
				{
					enumerator = GetRegexMatchedTagCollection(text, out lineLengthWithoutTags);
					match = null;
					if (enumerator.MoveNext())
					{
						match = (Match)enumerator.Current;
					}
				}
				float num4 = (float)(lineLengthWithoutTags - 1) * num * num2;
				int num5 = 0;
				int num6 = 0;
				while (num5 < text.Length)
				{
					if (flag && match != null && match.Index == num5)
					{
						num5 += match.Length - 1;
						num6--;
						num3 += match.Length;
						match = null;
						if (enumerator.MoveNext())
						{
							match = (Match)enumerator.Current;
						}
					}
					else
					{
						int index = num3 * 6;
						int index2 = num3 * 6 + 1;
						int index3 = num3 * 6 + 2;
						int index4 = num3 * 6 + 3;
						int index5 = num3 * 6 + 4;
						int num7 = num3 * 6 + 5;
						if (num7 > verts.Count - 1)
						{
							return;
						}
						UIVertex value = verts[index];
						UIVertex value2 = verts[index2];
						UIVertex value3 = verts[index3];
						UIVertex value4 = verts[index4];
						UIVertex value5 = verts[index5];
						UIVertex value6 = verts[num7];
						Vector3 vector = Vector3.right * (num * (float)num6 - num4);
						value.position += vector;
						value2.position += vector;
						value3.position += vector;
						value4.position += vector;
						value5.position += vector;
						value6.position += vector;
						verts[index] = value;
						verts[index2] = value2;
						verts[index3] = value3;
						verts[index4] = value4;
						verts[index5] = value5;
						verts[num7] = value6;
						num3++;
					}
					num5++;
					num6++;
				}
				num3++;
			}
		}

		private IEnumerator GetRegexMatchedTagCollection(string line, out int lineLengthWithoutTags)
		{
			MatchCollection matchCollection = Regex.Matches(line, "<b>|</b>|<i>|</i>|<size=.*?>|</size>|<color=.*?>|</color>|<material=.*?>|</material>");
			lineLengthWithoutTags = 0;
			int num = 0;
			if (matchCollection.Count > 0)
			{
				foreach (Match item in matchCollection)
				{
					num += item.Length;
				}
			}
			lineLengthWithoutTags = line.Length - num;
			return matchCollection.GetEnumerator();
		}
	}
}
