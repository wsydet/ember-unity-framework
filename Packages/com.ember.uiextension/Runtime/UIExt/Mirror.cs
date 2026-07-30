//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//
//using UnityEngine;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    [AddComponentMenu("UI/Effects/Mirror")]
//    [RequireComponent(typeof(Image))]
//    public class Mirror : BaseMeshEffect
//    {
//        public enum MirrorTypes
//        {
//            Horizontal,
//            Vertical,
//            Clockwise,
//        }
//        [SerializeField]
//        private MirrorTypes mirrorType;
//        [SerializeField]
//        private bool isReverse;
//        [SerializeField]
//        private bool keepImageSize;
//
//        Image img;
//
//        public MirrorTypes MirrowType { get { return mirrorType; } set { mirrorType = value; } }
//
//        public bool IsReverse { get { return isReverse; } set { isReverse = value; } }
//        public bool KeepImageSize { get { return keepImageSize; } set { keepImageSize = value; } }
//
//        protected override void OnEnable()
//        {
//            base.OnEnable();
//            if (!img)
//                img = GetComponent<Image>();
//        }
//
//        Vector2 ImageSize
//        {
//            get
//            {
//                return img.sprite.rect.size;
//            }
//        }
//        public override void ModifyMesh(VertexHelper vh)
//        {
//            if (!IsActive())
//            {
//                return;
//            }
//            List<UIVertex> vertexList = new List<UIVertex>();
//
//            vh.GetUIVertexStream(vertexList);
//
//            if (vertexList.Count == 6)
//            {
//                float width, height;
//                if (keepImageSize)
//                {
//                    var size = ImageSize;
//                    width = size.x;
//                    height = size.y;
//                }
//                else
//                {
//                    width = vertexList[2].position.x - vertexList[1].position.x;
//                    height = vertexList[1].position.y - vertexList[0].position.y;
//                    width = width / 2f;
//                    height = height / 2f;
//                }
//                Vector3 min = vertexList[0].position;
//                Vector3 max = vertexList[2].position;
//                switch (mirrorType)
//                {
//                    case MirrorTypes.Horizontal:
//                        MirrorHorizontal(vertexList, width, min, max);
//                        break;
//                    case MirrorTypes.Vertical:
//                        MirrorVertical(vertexList, height, min, max);
//                        break;
//                    case MirrorTypes.Clockwise:
//                        MirrorClockwise(vertexList, width, height, min, max);
//                        break;
//                }
//            }
//            vh.Clear();
//            vh.AddUIVertexTriangleStream(vertexList);
//        }
//
//        void MirrorClockwise(List<UIVertex> vertexList, float width, float height, Vector3 min, Vector3 max)
//        {
//            UIVertex tmp;
//            if (isReverse)
//            {
//                ChangeX(vertexList, 0, max.x - width);
//                ChangeX(vertexList, 1, max.x - width);
//                ChangeX(vertexList, 5, max.x - width);
//                ChangeY(vertexList, 0, max.y - height);
//                ChangeY(vertexList, 4, max.y - height);
//                ChangeY(vertexList, 5, max.y - height);
//
//                tmp = vertexList[3];
//                tmp.position.x = min.x;
//                vertexList.Add(tmp);
//                tmp = vertexList[4];
//                tmp.position.x = min.x;
//                vertexList.Add(tmp);
//                tmp = vertexList[5];
//                tmp.position.x = min.x + width;
//                vertexList.Add(tmp);
//
//                tmp = vertexList[0];
//                tmp.position.x = min.x + width;
//                vertexList.Add(tmp);
//                tmp = vertexList[1];
//                tmp.position.x = min.x + width;
//                vertexList.Add(tmp);
//                tmp = vertexList[2];
//                tmp.position.x = min.x;
//                vertexList.Add(tmp);
//
//                tmp = vertexList[1];
//                tmp.position.y = min.y;
//                vertexList.Add(tmp);
//                tmp = vertexList[0];
//                tmp.position.y = min.y + height;
//                vertexList.Add(tmp);
//                tmp = vertexList[4];
//                tmp.position.y = min.y + height;
//                vertexList.Add(tmp);
//
//                tmp = vertexList[4];
//                tmp.position.y = min.y + height;
//                vertexList.Add(tmp);
//                tmp = vertexList[3];
//                tmp.position.y = min.y;
//                vertexList.Add(tmp);
//                tmp = vertexList[1];
//                tmp.position.y = min.y;
//                vertexList.Add(tmp);
//
//                tmp = vertexList[1];
//                tmp.position.x = min.x + width;
//                tmp.position.y = min.y;
//                vertexList.Add(tmp);
//                tmp = vertexList[0];
//                tmp.position.x = min.x + width;
//                tmp.position.y = min.y + height;
//                vertexList.Add(tmp);
//                tmp = vertexList[4];
//                tmp.position.x = min.x;
//                tmp.position.y = min.y + height;
//                vertexList.Add(tmp);
//
//                tmp = vertexList[4];
//                tmp.position.x = min.x;
//                tmp.position.y = min.y + height;
//                vertexList.Add(tmp);
//                tmp = vertexList[3];
//                tmp.position.x = min.x;
//                tmp.position.y = min.y;
//                vertexList.Add(tmp);
//                tmp = vertexList[1];
//                tmp.position.x = min.x + width;
//                tmp.position.y = min.y;
//                vertexList.Add(tmp);
//            }
//            else
//            {
//                ChangeX(vertexList, 2, min.x + width);
//                ChangeX(vertexList, 3, min.x + width);
//                ChangeX(vertexList, 4, min.x + width);
//                ChangeY(vertexList, 1, min.y + height);
//                ChangeY(vertexList, 2, min.y + height);
//                ChangeY(vertexList, 3, min.y + height);
//
//                tmp = vertexList[3];
//                tmp.position.x = max.x - width;
//                vertexList.Add(tmp);
//                tmp = vertexList[4];
//                tmp.position.x = max.x - width;
//                vertexList.Add(tmp);
//                tmp = vertexList[5];
//                tmp.position.x = max.x;
//                vertexList.Add(tmp);
//
//                tmp = vertexList[0];
//                tmp.position.x = max.x;
//                vertexList.Add(tmp);
//                tmp = vertexList[1];
//                tmp.position.x = max.x;
//                vertexList.Add(tmp);
//                tmp = vertexList[2];
//                tmp.position.x = max.x - width;
//                vertexList.Add(tmp);
//
//                tmp = vertexList[1];
//                tmp.position.y = max.y - height;
//                vertexList.Add(tmp);
//                tmp = vertexList[0];
//                tmp.position.y = max.y;
//                vertexList.Add(tmp);
//                tmp = vertexList[4];
//                tmp.position.y = max.y;
//                vertexList.Add(tmp);
//
//                tmp = vertexList[4];
//                tmp.position.y = max.y;
//                vertexList.Add(tmp);
//                tmp = vertexList[3];
//                tmp.position.y = max.y - height;
//                vertexList.Add(tmp);
//                tmp = vertexList[1];
//                tmp.position.y = max.y - height;
//                vertexList.Add(tmp);
//
//                tmp = vertexList[1];
//                tmp.position.x = max.x;
//                tmp.position.y = max.y - height;
//                vertexList.Add(tmp);
//                tmp = vertexList[0];
//                tmp.position.x = max.x;
//                tmp.position.y = max.y;
//                vertexList.Add(tmp);
//                tmp = vertexList[4];
//                tmp.position.x = max.x - width;
//                tmp.position.y = max.y;
//                vertexList.Add(tmp);
//
//                tmp = vertexList[4];
//                tmp.position.x = max.x - width;
//                tmp.position.y = max.y;
//                vertexList.Add(tmp);
//                tmp = vertexList[3];
//                tmp.position.x = max.x - width;
//                tmp.position.y = max.y - height;
//                vertexList.Add(tmp);
//                tmp = vertexList[1];
//                tmp.position.x = max.x;
//                tmp.position.y = max.y - height;
//                vertexList.Add(tmp);
//            }
//        }
//
//        void MirrorVertical(List<UIVertex> vertexList, float height, Vector3 min, Vector3 max)
//        {
//            UIVertex tmp;
//            if (isReverse)
//            {
//                ChangeY(vertexList, 0, max.y - height);
//                ChangeY(vertexList, 4, max.y - height);
//                ChangeY(vertexList, 5, max.y - height);
//
//                tmp = vertexList[1];
//                tmp.position.y = min.y;
//                vertexList.Add(tmp);
//                tmp = vertexList[0];
//                tmp.position.y = min.y + height;
//                vertexList.Add(tmp);
//                tmp = vertexList[4];
//                tmp.position.y = min.y + height;
//                vertexList.Add(tmp);
//
//                tmp = vertexList[4];
//                tmp.position.y = min.y + height;
//                vertexList.Add(tmp);
//                tmp = vertexList[3];
//                tmp.position.y = min.y;
//                vertexList.Add(tmp);
//                tmp = vertexList[1];
//                tmp.position.y = min.y;
//                vertexList.Add(tmp);
//            }
//            else
//            {
//                ChangeY(vertexList, 1, min.y + height);
//                ChangeY(vertexList, 2, min.y + height);
//                ChangeY(vertexList, 3, min.y + height);
//
//                tmp = vertexList[1];
//                tmp.position.y = max.y - height;
//                vertexList.Add(tmp);
//                tmp = vertexList[0];
//                tmp.position.y = max.y; 
//                vertexList.Add(tmp);
//                tmp = vertexList[4];
//                tmp.position.y = max.y;
//                vertexList.Add(tmp);
//
//                tmp = vertexList[4];
//                tmp.position.y = max.y;
//                vertexList.Add(tmp);
//                tmp = vertexList[3];
//                tmp.position.y = max.y - height;
//                vertexList.Add(tmp);
//                tmp = vertexList[1];
//                tmp.position.y = max.y - height;
//                vertexList.Add(tmp);
//            }
//        }
//
//        void MirrorHorizontal(List<UIVertex> vertexList, float width, Vector3 min, Vector3 max)
//        {
//            UIVertex tmp;
//            if (isReverse)
//            {
//                ChangeX(vertexList, 0, max.x - width);
//                ChangeX(vertexList, 1, max.x - width);
//                ChangeX(vertexList, 5, max.x - width);
//
//                tmp = vertexList[3];
//                tmp.position.x = min.x;
//                vertexList.Add(tmp);
//                tmp = vertexList[4];
//                tmp.position.x = min.x;
//                vertexList.Add(tmp);
//                tmp = vertexList[5];
//                tmp.position.x = min.x + width;
//                vertexList.Add(tmp);
//
//                tmp = vertexList[0];
//                tmp.position.x = min.x + width;
//                vertexList.Add(tmp);
//                tmp = vertexList[1];
//                tmp.position.x = min.x + width;
//                vertexList.Add(tmp);
//                tmp = vertexList[2];
//                tmp.position.x = min.x;
//                vertexList.Add(tmp);
//            }
//            else
//            {
//                ChangeX(vertexList, 2, min.x + width);
//                ChangeX(vertexList, 3, min.x + width);
//                ChangeX(vertexList, 4, min.x + width);
//
//                tmp = vertexList[3];
//                tmp.position.x = max.x - width;
//                vertexList.Add(tmp);
//                tmp = vertexList[4];
//                tmp.position.x = max.x - width;
//                vertexList.Add(tmp);
//                tmp = vertexList[5];
//                tmp.position.x = max.x;
//                vertexList.Add(tmp);
//
//                tmp = vertexList[0];
//                tmp.position.x = max.x;
//                vertexList.Add(tmp);
//                tmp = vertexList[1];
//                tmp.position.x = max.x;
//                vertexList.Add(tmp);
//                tmp = vertexList[2];
//                tmp.position.x = max.x - width;
//                vertexList.Add(tmp);
//            }
//        }
//
//        private void ChangeX(List<UIVertex> verList, int index, float val)
//        {
//            UIVertex temp = verList[index];
//            temp.position.x = val;
//            verList[index] = temp;
//        }
//
//        private void ChangeY(List<UIVertex> verList, int index, float val)
//        {
//            UIVertex temp = verList[index];
//            temp.position.y = val;
//            verList[index] = temp;
//        }
//
//        private void ChangeColor(List<UIVertex> verList, int index, Color color)
//        {
//            UIVertex temp = verList[index];
//            temp.color *= color;
//            verList[index] = temp;
//        }
//    }
//}
