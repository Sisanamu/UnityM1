/****************************************************************************
*                                                                           *
* vCatchInputModule.cs                                                      *
*                                                                           *
* made by Willy.Lee                                                         *
*                                                                           *
*    Kee-Wan Lee, 2022-          e-mail : wiljwilj@hotmail.com              *
*                                                                           *
****************************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace vCatchStation
{
    public class vCatchInputModule : StandaloneInputModule
    {
#if UNITY_EDITOR
        static Rect[] s_rects = new Rect[8] {
            new Rect( 0.0f, 0.0f, 1024.0f, 768.0f),
            new Rect( 0.0f, 0.0f, 1024.0f, 768.0f),
            new Rect( 0.0f, 0.0f, 1024.0f, 768.0f),
            new Rect( 0.0f, 0.0f, 1024.0f, 768.0f),
            new Rect( 0.0f, 0.0f, 1024.0f, 768.0f),
            new Rect( 0.0f, 0.0f, 1024.0f, 768.0f),
            new Rect( 0.0f, 0.0f, 1024.0f, 768.0f),
            new Rect( 0.0f, 0.0f, 1024.0f, 768.0f) };
#endif //UNITY_EDITOR

        public void ProcessTouch(int targetDisplay, int id, float x, float y, vScreen vscn, bool pressed, bool released)
        {
#if UNITY_EDITOR
            Vector2 pos = new Vector2(s_rects[targetDisplay].width * x + s_rects[targetDisplay].x, s_rects[targetDisplay].height * (1.0f - y) + s_rects[targetDisplay].y);
#else //UNITY_EDITOR
            Vector2 pos = new Vector2(vscn.width * x + vscn.left, vscn.height * (1.0f - y) - vscn.top);
#endif //UNITY_EDITOR

            Input.simulateMouseWithTouches = true;

            bool pressed2;
            bool released2;
            var pointerData = GetTouchPointerEventData2(targetDisplay, new Touch()
            {
                fingerId = id + targetDisplay * 10000,
                position = pos,
                phase = (released ? TouchPhase.Ended : TouchPhase.Moved)
            }, out pressed2, out released2);

            ProcessTouchPress(pointerData, pressed, released);

            if (!released2)
            {
                ProcessMove(pointerData);
                ProcessDrag(pointerData);
            }
            else
                RemovePointerData(pointerData);
        }

        protected PointerEventData GetTouchPointerEventData2(int targetDisplay, Touch input, out bool pressed, out bool released)
        {
            PointerEventData pointerData;
            var created = GetPointerData(input.fingerId, out pointerData, true);

            pointerData.Reset();

            pressed = created || (input.phase == TouchPhase.Began);
            released = (input.phase == TouchPhase.Canceled) || (input.phase == TouchPhase.Ended);

            if (created)
                pointerData.position = input.position;

            if (pressed)
                pointerData.delta = Vector2.zero;
            else
                pointerData.delta = input.position - pointerData.position;

            pointerData.position = input.position;

            pointerData.button = PointerEventData.InputButton.Left;

            if (input.phase == TouchPhase.Canceled)
            {
                pointerData.pointerCurrentRaycast = new RaycastResult();
            }
            else
            {
                eventSystem.RaycastAll(pointerData, m_RaycastResultCache);

#if UNITY_EDITOR
                foreach (var r in m_RaycastResultCache)
                {
                    if (r.module == null || r.module.eventCamera == null)
                        continue;

                    s_rects[r.displayIndex] = r.module.eventCamera.pixelRect;
                }
#endif //UNITY_EDITOR

                var raycast = FindFirstRaycast2(targetDisplay, m_RaycastResultCache);
                pointerData.pointerCurrentRaycast = raycast;
                m_RaycastResultCache.Clear();
            }
            return pointerData;
        }

        protected static RaycastResult FindFirstRaycast2(int targetDisplay, List<RaycastResult> candidates)
        {
            var candidatesCount = candidates.Count;
            for (var i = 0; i < candidatesCount; ++i)
            {
                if (candidates[i].gameObject == null)
                    continue;
                if (candidates[i].displayIndex != targetDisplay)
                    continue;

                return candidates[i];
            }
            return new RaycastResult();
        }
    }
}
