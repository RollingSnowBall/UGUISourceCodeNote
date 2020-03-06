using System.Collections.Generic;
using UnityEngine.UI;

namespace UnityEngine.EventSystems
{
    /// <summary>
    /// Simple event system using physics raycasts.
    /// </summary>
    [AddComponentMenu("Event/Physics Raycaster")]
    [RequireComponent(typeof(Camera))]
    /// <summary>
    /// Raycaster for casting against 3D Physics components.
    /// </summary>
    public class PhysicsRaycaster : BaseRaycaster
    {
        /// <summary>
        /// Const to use for clarity when no event mask is set
        /// </summary>
        protected const int kNoEventMaskSet = -1;

        protected Camera m_EventCamera;

        /// <summary>
        /// Layer mask used to filter events. Always combined with the camera's culling mask if a camera is used.
        /// </summary>
        [SerializeField]
        protected LayerMask m_EventMask = kNoEventMaskSet;

        /// <summary>
        /// 射线可以穿透的物体的数量
        /// The max number of intersections allowed. 0 = allocating version anything else is non alloc.
        /// </summary>
        [SerializeField]
        protected int m_MaxRayIntersections = 0;
        protected int m_LastMaxRayIntersections = 0;

        RaycastHit[] m_Hits;

        protected PhysicsRaycaster()
        {}

        public override Camera eventCamera
        {
            get
            {
                if (m_EventCamera == null)
                    m_EventCamera = GetComponent<Camera>();
                return m_EventCamera ?? Camera.main;
            }
        }


        /// <summary>
        /// Depth used to determine the order of event processing.
        /// </summary>
        public virtual int depth
        {
            get { return (eventCamera != null) ? (int)eventCamera.depth : 0xFFFFFF; }
        }

        /// <summary>
        /// Event mask used to determine which objects will receive events.
        /// </summary>
        public int finalEventMask
        {
            get { return (eventCamera != null) ? eventCamera.cullingMask & m_EventMask : kNoEventMaskSet; }
        }

        /// <summary>
        /// Layer mask used to filter events. Always combined with the camera's culling mask if a camera is used.
        /// </summary>
        public LayerMask eventMask
        {
            get { return m_EventMask; }
            set { m_EventMask = value; }
        }

        /// <summary>
        /// Max number of ray intersection allowed to be found.
        /// </summary>
        /// <remarks>
        /// A value of zero will represent using the allocating version of the raycast function where as any other value will use the non allocating version.
        /// </remarks>
        public int maxRayIntersections
        {
            get { return m_MaxRayIntersections; }
            set { m_MaxRayIntersections = value; }
        }

        /// <summary>
        /// 相机到点的射线和距离
        /// Returns a ray going from camera through the event position and the distance between the near and far clipping planes along that ray.
        /// </summary>
        protected void ComputeRayAndDistance(PointerEventData eventData, out Ray ray, out float distanceToClipPlane)
        {
            ray = eventCamera.ScreenPointToRay(eventData.position);
            // compensate far plane distance - see MouseEvents.cs
            float projectionDirection = ray.direction.z;
            //这里获取的是这个相机在ray方向上的最长的距离
            distanceToClipPlane = Mathf.Approximately(0.0f, projectionDirection)
                ? Mathf.Infinity
                : Mathf.Abs((eventCamera.farClipPlane - eventCamera.nearClipPlane) / projectionDirection);
        }

        // 相机向点发出射线获取结果(照射到的物体等等)
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            // Cull ray casts that are outside of the view rect. (case 636595)
            if (eventCamera == null || !eventCamera.pixelRect.Contains(eventData.position))
                return;

            Ray ray;
            float distanceToClipPlane;
            ComputeRayAndDistance(eventData, out ray, out distanceToClipPlane);

            int hitCount = 0;

            if (m_MaxRayIntersections == 0)
            {
                //获取Physics.RaycastAll的反射方法
                if (ReflectionMethodsCache.Singleton.raycast3DAll == null)
                    return;
                //反射调用Physics.RaycastAll方法 获取射线碰到的所有物体，然后获取最近的第一个物体的距离hitDIstance
                m_Hits = ReflectionMethodsCache.Singleton.raycast3DAll(ray, distanceToClipPlane, finalEventMask);
                hitCount = m_Hits.Length;
            }
            else
            {
                if (ReflectionMethodsCache.Singleton.getRaycastNonAlloc == null)
                    return;
                // 获取一定数量的射线碰到的物体
                if (m_LastMaxRayIntersections != m_MaxRayIntersections)
                {
                    m_Hits = new RaycastHit[m_MaxRayIntersections];
                    m_LastMaxRayIntersections = m_MaxRayIntersections;
                }

                hitCount = ReflectionMethodsCache.Singleton.getRaycastNonAlloc(ray, m_Hits, distanceToClipPlane, finalEventMask);
            }

            if (hitCount > 1)
                System.Array.Sort(m_Hits, (r1, r2) => r1.distance.CompareTo(r2.distance));

            if (hitCount != 0)
            {
                for (int b = 0, bmax = hitCount; b < bmax; ++b)
                {
                    var result = new RaycastResult
                    {
                        gameObject = m_Hits[b].collider.gameObject,
                        module = this,
                        distance = m_Hits[b].distance,
                        worldPosition = m_Hits[b].point,
                        worldNormal = m_Hits[b].normal,
                        screenPosition = eventData.position,
                        index = resultAppendList.Count,
                        sortingLayer = 0,
                        sortingOrder = 0
                    };
                    resultAppendList.Add(result);
                }
            }
        }
    }
}
