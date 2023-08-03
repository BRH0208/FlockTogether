#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(DrivewayTileset))]
    [CanEditMultipleObjects]
    public class EditorForDriveway : RuleTileEditor
    {
        public Sprite LargeRoad;
        public Sprite SmallDriveway;
        public Sprite LargeDriveway;
		public Sprite SmallDrivewayM;
		public Sprite LargeDrivewayM;
		

        public override void RuleOnGUI(Rect rect, Vector3Int position, int neighbor)
        {
            switch (neighbor)
            {
                case DrivewayTileset.Neighbor.LargeRoad:
                    GUIDrawSprite(rect, LargeRoad);
                    return;
                case DrivewayTileset.Neighbor.SmallDriveway:
                    GUIDrawSprite(rect, SmallDriveway);
                    return;
                case DrivewayTileset.Neighbor.SmallDrivewayMirror:
                    GUIDrawSprite(rect, SmallDrivewayM);
                    return;
				case DrivewayTileset.Neighbor.LargeDriveway:
                    GUIDrawSprite(rect, LargeDriveway);
                    return;
                case DrivewayTileset.Neighbor.LargeDrivewayMirror:
                    GUIDrawSprite(rect, LargeDrivewayM);
                    return;
            }

            base.RuleOnGUI(rect, position, neighbor);
        }
		
		// From https://forum.unity.com/threads/try-to-show-a-sprite-in-gui-drawtexture.281793/
		// Created by user imprity
		public static void GUIDrawSprite(Rect rect, Sprite sprite){
            Rect spriteRect = sprite.rect;
            Texture2D tex = sprite.texture;
            GUI.DrawTextureWithTexCoords(rect, tex, new Rect(spriteRect.x / tex.width, spriteRect.y / tex.height, spriteRect.width/ tex.width, spriteRect.height / tex.height));
        }

    }
}
#endif