using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Argus.Audio.Interaction
{
    [CreateAssetMenu(menuName = "SAO/Audio/Footstep Sounds")]
    public class FootstepSounds : ScriptableObject
    {
        [SerializeField] public GroundType defaultGroundType;
        [SerializeField] public List<GroundType> groundTypes = new List<GroundType>();

        [Serializable]
        public class GroundType
        {
            public string name = "New Ground Type";
            public List<AudioClip> clips = new List<AudioClip>();

            public void DrawEditor(Action action)
            {
                using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    name = EditorGUILayout.TextField(name, GUILayout.Width(160));

                    using (new GUILayout.VerticalScope())
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Add Clip"))
                            {
                                clips.Add(null);
                            }

                            action.Invoke();
                        }

                        for (int i = 0; i < clips.Count; i++)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                clips[i] = (AudioClip) EditorGUILayout.ObjectField(clips[i], typeof(AudioClip), false);

                                if (GUILayout.Button("X", GUILayout.Width(20)))
                                {
                                    clips.RemoveAt(i);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    [CustomEditor(typeof(FootstepSounds))]
    public class FootstepSoundsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            FootstepSounds footstepSounds = (FootstepSounds) target;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Default Ground Type");
            footstepSounds.defaultGroundType.DrawEditor(() => { });

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Ground Types");
            foreach (var groundType in footstepSounds.groundTypes)
            {
                groundType.DrawEditor(() =>
                {
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        footstepSounds.groundTypes.Remove(groundType);
                        GUIUtility.ExitGUI();
                    }
                });
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Add Ground Type"))
            {
                footstepSounds.groundTypes.Add(new FootstepSounds.GroundType());
            }

            if (GUILayout.Button("Push to Player Trackers"))
            {
                //PlayerTrackerTools.PushFootstepSounds();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(footstepSounds);
            }
        }
    }
}