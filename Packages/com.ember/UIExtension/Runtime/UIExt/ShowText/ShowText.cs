//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine;
//
//namespace Burner.UIExtension
//{
//	[ExecuteAlways]
//	public class ShowText : MonoBehaviour
//	{
//		public class TextElement
//		{
//			public SpriteRenderer spriteRenderer;
//			public GameObject obj;
//			public float Width => spriteRenderer.sprite?.bounds.size.x ?? 0;
//			public void Play() => spriteRenderer.gameObject.SetActive(true);
//			public void Reset() => spriteRenderer.gameObject.SetActive(false);
//
//			public TextElement(Transform transform)
//			{
//				obj = transform.gameObject;
//				var ani = obj.GetComponent<Animation>();
//				if (ani != null)
//				{
//					ani.enabled = false;
//				}
//				spriteRenderer = obj.GetComponentInChildren<SpriteRenderer>(true);
//			}
//		}
//
//		[Range(1, 16)]
//		public int maxCharCount = 7;
//		public string text;
//		private string oldText;
//
//		public ShowTextSource showTextSource;
//
//		private TextElement[] textElements;
//
//		public void Clear()
//		{
//			textElements = null;
//		}
//
//		public void Reset()
//		{
//			text = "";
//			oldText = "";
//			GetComponent<Animation>()?.Rewind();
//			if (textElements != null)
//			{
//				foreach (var elem in textElements) elem.Reset();
//			}
//		}
//
//		private void Build()
//		{
//			if (textElements == null || textElements.Length != maxCharCount)
//			{
//				maxCharCount = Mathf.Min(maxCharCount, transform.childCount);
//
//				textElements = new TextElement[maxCharCount];
//				for (int i = 0; i < maxCharCount; i++)
//				{
//					textElements[i] = new TextElement(transform.GetChild(i));
//				}
//			}
//		}
//		
//		private void OnTextChange()
//		{
//			if (showTextSource == null)
//			{
//				return;
//			}
//
//			Build();
//			float width = 0;
//			for (int i = 0; i < maxCharCount; i++)
//			{
//				var elem = textElements[i];
//
//				if (i < text.Length)
//				{
//					var sp = showTextSource.GetCharSprite(text[i]);
//					if (sp != null)
//					{
//						elem.spriteRenderer.sprite = sp;
//						width += elem.Width;
//					}
//				}
//				else
//				{
//					elem.spriteRenderer.sprite = null;
//					elem.spriteRenderer.gameObject.SetActive(false);
//				}
//			}
//
//			float offset = -width * 0.5f;
//			for (int i = 0; i < text.Length && i < maxCharCount; i++)
//			{
//				if (textElements[i].spriteRenderer.sprite == null)
//				{
//					continue;
//				}
//				var p = textElements[i].spriteRenderer.gameObject.transform.localPosition;
//				if (i >= 1)
//				{
//					offset += (textElements[i].Width + textElements[i - 1].Width) * 0.5f;
//				}
//				else
//				{
//					offset += textElements[i].Width * 0.5f;
//				}
//				p.x = offset;
//				textElements[i].spriteRenderer.gameObject.transform.localPosition = p;
//				textElements[i].Play();
//			}
//		}
//
//		void Update()
//		{
//			if (oldText != text)
//			{
//				OnTextChange();
//				oldText = text;
//				GetComponent<Animation>()?.Play();
//			}
//		}
//	}
//}
