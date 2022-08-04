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

        public QuadTreeSpaceSplitter(float looseSize)
        {
            m_looseSize = looseSize;
        }

        public int CalculateTreeDepth(Bounds bounds, float chunkSize)
        {
            float maxLength = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            int depth = 1;

            while (maxLength > chunkSize)
            {
                depth += 1;
                maxLength *= 0.5f;
            }

            return depth;
        }
        public SpaceNode CreateSpaceTree(Bounds initBounds, float chunkSize, Transform transform, List<GameObject> targetObjects, Action<float> onProgress)
        {
            SpaceNode rootNode = new SpaceNode();
            rootNode.Bounds = initBounds;

            if ( onProgress != null)
                onProgress(0.0f);

			//space split first
			Stack<SpaceNode> nodeStack = new Stack<SpaceNode>();
			nodeStack.Push(rootNode);
		
			while(nodeStack.Count > 0 )
			{
				SpaceNode node = nodeStack.Pop();
				if ( node.Bounds.size.x > chunkSize )
				{
                    List<SpaceNode> childNodes = CreateChildSpaceNodes(node);
					
					for ( int i = 0; i < childNodes.Count; ++i )
                    {
                        childNodes[i].ParentNode = node;
						nodeStack.Push(childNodes[i]);
					}
						
				}
			}

            if (targetObjects == null)
                return rootNode;

            for (int oi = 0; oi < targetObjects.Count; ++oi)
            {
                Bounds? objectBounds = CalculateBounds(targetObjects[oi], transform);
                if (objectBounds == null)
                    continue;

                SpaceNode target = rootNode;

                while (true)
                {
                    if (target.HasChild())
                    {
                        //the object can be in the over 2 nodes.
                        //we should figure out which node is more close with the object.
                        int nearestChild = -1;
                        float nearestDistance = float.MaxValue;

                        for (int ci = 0; ci < target.GetChildCount(); ++ci)
                        {
                            if (objectBounds.Value.IsPartOf(target.GetChild(ci).Bounds))
                            {
                                float dist = Vector3.Distance(target.GetChild(ci).Bounds.center, objectBounds.Value.center);

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
                            target = target.GetChild(nearestChild);
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

        private Bounds? CalculateBounds(GameObject obj, Transform transform)
        {
            MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
                return null;

            Bounds result = Utils.BoundsUtils.CalcLocalBounds(renderers[0], transform);
            for (int i = 1; i < renderers.Length; ++i)
            {
                result.Encapsulate(Utils.BoundsUtils.CalcLocalBounds(renderers[i], transform));
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