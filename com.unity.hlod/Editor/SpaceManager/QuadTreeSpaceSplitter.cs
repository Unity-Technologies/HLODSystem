using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.SpaceManager
{
    public static class BoundsExtension
    {
        public static bool IsPartOf(this Bounds bounds, Bounds target)
        {
            return (double) bounds.min.x >= (double) target.min.x &&
                   (double) bounds.max.x <= (double) target.max.x &&
                   (double) bounds.min.y >= (double) target.min.y &&
                   (double) bounds.max.y <= (double) target.max.y &&
                   (double) bounds.min.z >= (double) target.min.z &&
                   (double) bounds.max.z <= (double) target.max.z;
        }
    }
    public class QuadTreeSpaceSplitter : ISpaceSplitter
    {
        private float m_looseSize;
        private float m_minSize;
        private Vector3 m_rootPosition;

        public QuadTreeSpaceSplitter(Vector3 rootPosition, float looseSize, float minSize)
        {
            m_rootPosition = rootPosition;
            m_looseSize = looseSize;
            m_minSize = minSize;
        }
        public SpaceNode CreateSpaceTree(Bounds initBounds, List<GameObject> targetObjects, Action<float> onProgress)
        {
            SpaceNode rootNode = new SpaceNode();
            rootNode.Bounds = initBounds;

            if ( onProgress != null)
                onProgress(0.0f);

            for (int oi = 0; oi < targetObjects.Count; ++oi)
            {
                Bounds? objectBounds = CalculateBounds(targetObjects[oi]);
                if (objectBounds == null)
                    continue;

                SpaceNode target = rootNode;

                while (true)
                {
                    if (target.Bounds.size.x > m_minSize)
                    {
                        if (target.ChildTreeNodes == null)
                        {
                            target.ChildTreeNodes = CreateChildSpaceNodes(target);
                        }


                        //the object can be in the over 2 nodes.
                        //we should figure out which node is more close with the object.
                        int nearestChild = -1;
                        float nearestDistance = float.MaxValue;

                        for (int ci = 0; ci < target.ChildTreeNodes.Count; ++ci)
                        {
                            if (objectBounds.Value.IsPartOf(target.ChildTreeNodes[ci].Bounds))
                            {
                                float dist = Vector3.Distance(target.ChildTreeNodes[ci].Bounds.center, objectBounds.Value.center);

                                if (dist < nearestDistance)
                                {
                                    nearestChild = ci;
                                    nearestDistance = dist;
                                }
                            }
                        }

                        //We should find out it until we get the fit size from the bottom.
                        //this means the object is small to add in the current node.
                        if (nearestChild >= 0)
                        {
                            target = target.ChildTreeNodes[nearestChild];
                            continue;
                        }
                    }

                    target.Objects.Add(targetObjects[oi]);
                    break;
                }        
                
                if ( onProgress != null)
                    onProgress((float)oi/ (float)targetObjects.Count);
            }
            
            return rootNode;
        }


        private Bounds CalcBounds(Renderer renderer)
        {
            Bounds bounds = renderer.bounds;
            bounds.center -= m_rootPosition;

            return bounds;
        }
        private Bounds? CalculateBounds(GameObject obj)
        {
            MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
                return null;

            Bounds result = CalcBounds(renderers[0]);
            for (int i = 1; i < renderers.Length; ++i)
            {
                result.Encapsulate(CalcBounds(renderers[i]));
            }

            return result;
        }

        

        private List<SpaceNode> CreateChildSpaceNodes(SpaceNode parentNode)
        {
            List<SpaceNode> childSpaceNodes = new List<SpaceNode>(4);
            
            float size = parentNode.Bounds.size.x;
            float extend = size * 0.5f;
            float offset = extend * 0.5f;

            Vector3 center = parentNode.Bounds.center;
            Vector3 looseBoundsSize = new Vector3(extend + m_looseSize, size, extend + m_looseSize);

            childSpaceNodes.Add(
                SpaceNode.CreateSpaceNodeWithBounds(
                    new Bounds(center + new Vector3(-offset, 0.0f, -offset), looseBoundsSize)
                ));
            childSpaceNodes.Add(
                SpaceNode.CreateSpaceNodeWithBounds(
                    new Bounds(center + new Vector3(-offset, 0.0f, offset), looseBoundsSize)
                ));
            childSpaceNodes.Add(
                SpaceNode.CreateSpaceNodeWithBounds(
                    new Bounds(center + new Vector3(offset, 0.0f, -offset), looseBoundsSize)
                ));
            childSpaceNodes.Add(
                SpaceNode.CreateSpaceNodeWithBounds(
                    new Bounds(center + new Vector3(offset, 0.0f, offset), looseBoundsSize)
                ));
            
            return childSpaceNodes;
        }



        
    }

}