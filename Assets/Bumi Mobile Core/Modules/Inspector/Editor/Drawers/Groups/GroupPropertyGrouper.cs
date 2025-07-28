using UnityEditor;

namespace BumiMobile
{
    [PropertyGrouper(typeof(GroupAttribute))]
    public class GroupPropertyGrouper : PropertyGrouper
    {
        public override void BeginGroup(CustomInspector editor, string groupID, string label)
        {
            EditorGUILayout.BeginVertical();
        }

        public override void EndGroup()
        {
            EditorGUILayout.EndVertical();
        }
    }
}
