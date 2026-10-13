using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI.Editor
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp-Editor")]
    [CustomEditor(typeof(FactionRelationsSO))]
    public sealed class FactionRelationsSOEditor : UnityEditor.Editor
    {
        private const float RowLabelWidth = 150f;
        private const float CellWidth = 112f;

        private SerializedProperty damagePermissions;
        private Vector2 scrollPosition;

        private void OnEnable()
        {
            damagePermissions = serializedObject.FindProperty("damagePermissions");
            EnsureMatrixSize();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EnsureMatrixSize();

            EditorGUILayout.HelpBox(
                "Rows are attackers and columns are targets. Green means damage is allowed. " +
                "The matrix grows automatically when a FactionType is added.",
                MessageType.Info);

            DrawBulkActions();
            EditorGUILayout.Space(4f);

            FactionType[] factions = GetFactions();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawHeader(factions);

            foreach (FactionType attacker in factions)
                DrawRow(attacker, factions);

            EditorGUILayout.EndScrollView();

            if (serializedObject.ApplyModifiedProperties())
                EditorUtility.SetDirty(target);
        }

        private void DrawBulkActions()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear All", GUILayout.Width(90f)))
                SetAll(FactionMask.None);

            if (GUILayout.Button("Allow All", GUILayout.Width(90f)))
                SetAll(FactionMask.All);

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawHeader(FactionType[] factions)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(
                "Attacker ↓  /  Target →",
                EditorStyles.miniBoldLabel,
                GUILayout.Width(RowLabelWidth));

            foreach (FactionType faction in factions)
            {
                GUILayout.Label(
                    ObjectNames.NicifyVariableName(faction.ToString()),
                    EditorStyles.miniBoldLabel,
                    GUILayout.Width(CellWidth));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRow(FactionType attacker, FactionType[] targets)
        {
            int attackerIndex = (int)attacker;
            if (attackerIndex < 0 || attackerIndex >= damagePermissions.arraySize)
                return;

            SerializedProperty row = damagePermissions.GetArrayElementAtIndex(attackerIndex);
            int mask = row.intValue;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(
                ObjectNames.NicifyVariableName(attacker.ToString()),
                EditorStyles.boldLabel,
                GUILayout.Width(RowLabelWidth));

            foreach (FactionType targetFaction in targets)
            {
                int flag = 1 << (int)targetFaction;
                bool allowed = (mask & flag) != 0;
                Color previousColor = GUI.backgroundColor;
                GUI.backgroundColor = allowed
                    ? new Color(0.45f, 0.9f, 0.5f)
                    : new Color(0.9f, 0.45f, 0.45f);

                var content = new GUIContent(
                    allowed ? "Allowed" : "Blocked",
                    $"{attacker} can damage {targetFaction}");
                bool nextAllowed = GUILayout.Toggle(
                    allowed,
                    content,
                    EditorStyles.miniButton,
                    GUILayout.Width(CellWidth));

                GUI.backgroundColor = previousColor;

                if (nextAllowed == allowed)
                    continue;

                if (nextAllowed)
                    mask |= flag;
                else
                    mask &= ~flag;
            }

            row.intValue = mask;
            EditorGUILayout.EndHorizontal();
        }

        private void SetAll(FactionMask value)
        {
            for (int i = 0; i < damagePermissions.arraySize; i++)
                damagePermissions.GetArrayElementAtIndex(i).intValue = (int)value;
        }

        private void EnsureMatrixSize()
        {
            if (damagePermissions == null)
                return;

            int requiredSize = 0;
            foreach (FactionType faction in GetFactions())
                requiredSize = Mathf.Max(requiredSize, (int)faction + 1);

            if (damagePermissions.arraySize != requiredSize)
                damagePermissions.arraySize = requiredSize;
        }

        private static FactionType[] GetFactions()
        {
            return (FactionType[])Enum.GetValues(typeof(FactionType));
        }
    }
}
