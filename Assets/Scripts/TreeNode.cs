using System.Collections;
using System.Collections.Generic;

public class TreeNode<T>
{
    public T value;
    
    public TreeNode<T> parent;
    public TreeNode<T>[] children;
    
    public bool IsLeaf => children == null || children.Length == 0;

    public TreeNode(T value)
    {
        this.value = value;
    }

    #region IEnumerables
    public IEnumerable<T> DepthFirstTopDown()
    {
        yield return value;

        if (!IsLeaf)
        {
            foreach (var child in children)
            {
                foreach (var childValue in child.DepthFirstTopDown())
                {
                    yield return childValue;
                }
            }
        }
    }

    public IEnumerable<T> DepthFirstBottomUp()
    {
        if (!IsLeaf)
        {
            foreach (var child in children)
            {
                foreach (var childValue in child.DepthFirstBottomUp())
                {
                    yield return childValue;
                }
            }
        }
        
        yield return value;
    }

    public IEnumerable<T> Leaves()
    {
        if (IsLeaf)
        {
            yield return value;
        }
        else
        {
            foreach (var child in children)
            {
                foreach (var childValue in child.Leaves())
                {
                    yield return childValue;
                }
            }
        }
    }
    #endregion
}
