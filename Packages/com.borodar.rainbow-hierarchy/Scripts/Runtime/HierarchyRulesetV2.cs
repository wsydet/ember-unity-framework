using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Borodar.RainbowHierarchy.HierarchyRule;

namespace Borodar.RainbowHierarchy
{
	[ExecuteInEditMode]
	[HelpURL(AssetInfo.HELP_URL)]
	public class HierarchyRulesetV2 : MonoBehaviour
	{
		public const int VERSION = 2;
		public const string EXTENSION = "rhset";

		private const string RULESET_OBJ_NAME = "RainbowHierarchyRuleset";

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		public static readonly List<HierarchyRulesetV2> Instances = new List<HierarchyRulesetV2>();

		public static Action OnRulesetChangeCallback;

		// Editor

		public List<HierarchyRule> Rules = new List<HierarchyRule>();

		// Internal

		private Scene _scene;
		
		//---------------------------------------------------------------------
		// Messages
		//---------------------------------------------------------------------

		private void Awake()
		{
			if (Application.isEditor || !Application.isPlaying) return;

			Instances.Remove(this);
			DestroyImmediate(gameObject);
		}

		private void OnEnable()
		{
			_scene = gameObject.scene;
			UpdateOrdinals();

			Instances.Add(this);
		}

		private void OnDisable()
		{
			Instances.Remove(this);
		}

		[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Local")]
		private void OnRulesetChange()
		{
			UpdateOrdinals();
			OnRulesetChangeCallback();
		}
		
		//---------------------------------------------------------------------
		// Public
		//---------------------------------------------------------------------

		public static HierarchyRulesetV2 GetRulesetByScene(Scene scene, bool createIfNotExist = false)
		{
			var existingRuleset = Instances.FirstOrDefault(ruleset => ruleset._scene == scene);
			if (existingRuleset != null || !createIfNotExist) return existingRuleset;
			
			var newRuleset = CreateRuleset(scene);
			return newRuleset;
		}

		public HierarchyRule GetRule(GameObject match)
		{
			for (var i = Rules.Count - 1; i >= 0; i--)
			{
				var currentRule = Rules[i];

				if (currentRule.Type == KeyType.Name && currentRule.Name == match.name) return currentRule;
				if (currentRule.Type == KeyType.Object && currentRule.GameObject == match) return currentRule;
			}

			return null;
		}

		public void AddRule(HierarchyRule rule)
		{
			var newRule = new HierarchyRule(rule);
			Rules.Add(newRule);
			OnRulesetChange();
		}

		public void RemoveAll(GameObject match, KeyType type)
		{
			if (match == null) return;
			
			Rules.RemoveAll(x =>
				type == KeyType.Object && x.GameObject == match ||
				type == KeyType.Name && x.Name == match.name);

			OnRulesetChange();
		}

		public void UpdateRule(GameObject selectedObject, HierarchyRule rule)
		{
			var existingRule = GetRule(selectedObject);

			if (existingRule != null)
			{
				if (rule.HasAtLeastOneTexture())
				{
					existingRule.CopyFrom(rule);
					OnRulesetChange();
				}
				else
				{
					RemoveAll(selectedObject, existingRule.Type);
				}
			}
			else
			{
				if (rule.HasAtLeastOneTexture()) AddRule(rule);
			}
		}
		
		//---------------------------------------------------------------------
		// Helpers
		//---------------------------------------------------------------------

		private static HierarchyRulesetV2 CreateRuleset(Scene scene)
		{
			var rulesetObject = new GameObject {name = RULESET_OBJ_NAME};
			if (rulesetObject.scene != scene) SceneManager.MoveGameObjectToScene(rulesetObject, scene);

			var ruleset = rulesetObject.AddComponent<HierarchyRulesetV2>();
			ruleset.gameObject.tag = "EditorOnly";
			return ruleset;
		}

		private void UpdateOrdinals()
		{
			for (var i = 0; i < Rules.Count; i++)
			{
				Rules[i].Ordinal = i;
			}
		}
	}
}