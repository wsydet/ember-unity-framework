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
//	public class ShowTextInstance : MonoBehaviour
//	{
//		public string text;
//		public string fontName;
//		public Vector2 aniBeginAndEnd;
//		public SystemLanguage language = SystemLanguage.ChineseSimplified;
//		private string oldText;
//		private System.Action finishCallback;
//		private float finishTime;
//		private MaterialPropertyBlock block;
//		public ShowTextInstanceSource source;
//
//		void Update()
//		{
//			if (oldText != text)
//			{
//				CreateTextData();
//				oldText = text;
//			}
//			if (finishCallback != null && Time.time > finishTime)
//			{
//				finishCallback.Invoke();
//				finishCallback = null;
//			}
//		}
//
//		private float GetTime()
//		{
//			if (Application.IsPlaying(gameObject))
//			{
//				return Time.timeSinceLevelLoad;
//			}
//			else
//			{
//				var t = Shader.GetGlobalVector("_Time");
//				return t.y;
//			}
//		}
//
//		public void Stop(bool processCallback)
//		{
//			var render = GetComponent<MeshRenderer>();
//			if (render != null && source != null)
//			{
//				if (block == null)
//				{
//					block = new MaterialPropertyBlock();
//				}
//				var textArg = render.sharedMaterial.GetVector("_textArg");
//				float frameRate = textArg.x;//60
//				float useTime = (aniBeginAndEnd.y - aniBeginAndEnd.x) / frameRate;
//				render.GetPropertyBlock(block);
//				block.SetVector("_beginTime", new Vector4(GetTime() - useTime, aniBeginAndEnd.x, aniBeginAndEnd.y, 0));
//				render.SetPropertyBlock(block);
//			}
//			if (processCallback && finishCallback != null)
//			{
//				this.finishCallback.Invoke();
//			}
//		}
//
//		public void Play(System.Action finishCallback)
//		{
//			this.finishCallback = finishCallback;
//			var render = GetComponent<MeshRenderer>();
//			if (render == null || source == null)
//			{
//				this.finishCallback?.Invoke();
//				this.finishCallback = null;
//				return;
//			}
//			if (block == null)
//			{
//				block = new MaterialPropertyBlock();
//			}
//			var textArg = render.sharedMaterial.GetVector("_textArg");
//			float frameRate = textArg.x;//60
//			float useTime = (aniBeginAndEnd.y - aniBeginAndEnd.x) / frameRate;
//			finishTime = Time.time + useTime;
//			render.GetPropertyBlock(block);
//			block.SetVector("_beginTime", new Vector4(GetTime(), aniBeginAndEnd.x, aniBeginAndEnd.y, 0));
//			render.SetPropertyBlock(block);
//		}
//		
//		private string GetTextWithType(char c)
//		{
//			return fontName + "_" + c;
//		}
//
//		public void CreateTextData()
//		{
//			var render = GetComponent<MeshRenderer>();
//			if (render == null || source == null)
//			{
//				return;
//			}
//			if (block == null)
//			{
//				block = new MaterialPropertyBlock();
//			}
//			render.GetPropertyBlock(block);
//			float totalWidth = 0;
//			for (int i = 0; i < text.Length; i++)
//			{
//				var element = source.GetCharIndex(GetTextWithType(text[i]), language);
//				if (element != null)
//				{
//					totalWidth += element.Width;
//				}
//			}
//			float offset = -totalWidth * 0.5f;
//			Matrix4x4 posMatrix = new Matrix4x4();
//			Matrix4x4 widthMatrix = new Matrix4x4();
//			Matrix4x4 codeMatrix = new Matrix4x4();
//			for (int i = 0; i < 16; i++)
//			{
//				if (i < text.Length)
//				{
//					var element = source.GetCharIndex(GetTextWithType(text[i]), language);
//					if (element == null)
//					{
//						continue;
//					}
//					if (i - 1 >= 0)
//					{
//						var beforeElement = source.GetCharIndex(GetTextWithType(text[i - 1]), language);
//						if (beforeElement != null)
//						{
//							offset += (element.Width + beforeElement.Width) * 0.5f;
//						}
//					}
//					else
//					{
//						offset += element.Width * 0.5f;
//					}
//					posMatrix[i] = offset;
//					widthMatrix[i] = element.WidthScale;
//					codeMatrix[i] = element.index;
//				}
//				else
//				{
//					codeMatrix[i] = 1023;
//				}
//			}
//			block.SetMatrix("_pos", posMatrix);
//			block.SetMatrix("_width", widthMatrix);
//			block.SetMatrix("_code", codeMatrix);
//			block.SetVector("_beginTime", new Vector4(GetTime(), aniBeginAndEnd.x, aniBeginAndEnd.y, 0));
//			render.SetPropertyBlock(block);
//		}
//	}
//}
