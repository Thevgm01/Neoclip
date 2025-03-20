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
}
