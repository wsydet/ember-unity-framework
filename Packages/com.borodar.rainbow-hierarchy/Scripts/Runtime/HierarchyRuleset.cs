using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Borodar.RainbowHierarchy
{
	[ExecuteInEditMode]
	[HelpURL(AssetInfo.HELP_URL)]
	public class HierarchyRuleset : MonoBehaviour
	{
		public const int VERSION = 1;

		[FormerlySerializedAs("HierarchyItems")]
		public List<HierarchyRule> Rules = new List<HierarchyRule>();

		//---------------------------------------------------------------------
		// Messages
		//---------------------------------------------------------------------

		private void Awake()
		{
			HierarchyEditorProxy.UpdateOldRuleset?.Invoke(gameObject);
		}
	}
}