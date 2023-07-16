using UnityEngine;

namespace SmartRooms.Utils
{
	public static class TransformUtils
	{
		/// <summary>
		/// Extension method for transform which destroys all direct children GameObjects.
		/// </summary>
		/// <param name="parent"></param>
		public static void DestroyAllChildren(this Transform parent)
		{
			for (int i = parent.childCount - 1; i >= 0; i--)
			{
				Transform child = parent.GetChild(i);
				
				if (Application.isPlaying)
				{
					Object.Destroy(child.gameObject);
				}
				else
				{
					Object.DestroyImmediate(child.gameObject);
				}
			}
		}
		
		/// <summary>
		/// Extension method for transform which sets its global scale.
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="globalScale"></param>
		public static void SetGlobalScale(this Transform transform, Vector3 globalScale)
		{
			transform.localScale = Vector3.one;
			transform.localScale = new Vector3 (globalScale.x/transform.lossyScale.x, globalScale.y/transform.lossyScale.y, globalScale.z/transform.lossyScale.z);
		}
	}
}