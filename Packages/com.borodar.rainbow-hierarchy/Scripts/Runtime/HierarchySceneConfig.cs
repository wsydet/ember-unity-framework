using System.Collections.Generic;
using UnityEngine;

namespace Borodar.RainbowHierarchy
{
	// Required for compatibility reasons with previous versions of the asset (v2.2 or less)

	[ExecuteInEditMode]
	[HelpURL(AssetInfo.HELP_URL)]
	public class HierarchySceneConfig : MonoBehaviour
	{
		public List<HierarchyRule> HierarchyItems = new List<HierarchyRule>();

		private void Awake()
		{
			if (!Application.isEditor && Application.isPlaying)
			{
				DestroyImmediate(gameObject);
			}
			else
			{
				// Add new ruleset and copy all items
				var ruleset = gameObject.AddComponent<HierarchyRulesetV2>();
				ruleset.Rules = HierarchyItems;

				// Destroy old config
				DestroyImmediate(this);
			}
		}
	}
}
