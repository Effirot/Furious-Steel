using System.Collections;
using CharacterSystem.DamageMath;
using CharacterSystem.Interactions;
using CharacterSystem.Objects;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CharacterSystem.Objects
{
    public class DummyNetworkCharacter : NetworkCharacter
    {
        [Space]
        [Header("Dummy")]
        [SerializeField]
        public TMP_Text reportLabel;
        
        [SerializeField]
        public bool resetTimer = true;


        private Vector3 returnPosition;
        private float angle;

        private Coroutine resetRoutine;

        public void Reset()
        {
            SetPosition(returnPosition); 
            SetAngle(angle); 

            Push(Vector3.up / 5);

            health = maxHealth;

            if (reportLabel != null)
            {
                reportLabel.text = "";
            }

            if (resetRoutine != null)
            {
                StopCoroutine(resetRoutine);
                resetRoutine = null;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            returnPosition = transform.position;
            lookVector = new Vector2(transform.forward.x, transform.forward.z);
            angle = Quaternion.LookRotation(new (lookVector.x, 0, lookVector.y)).y;

            Reset();
        }

        public override bool Hit(ref Damage damage)
        {
            lookVector = -new Vector2(damage.pushDirection.x, damage.pushDirection.z);

            if (damage.pushDirection.magnitude > 0)
            {
                if (resetRoutine != null)
                {
                    StopCoroutine(resetRoutine);
                    resetRoutine = null;
                }

                if (resetTimer)
                {
                    resetRoutine = StartCoroutine(ReturnTimer());
                }
            } 

            if (reportLabel != null)
            {
                reportLabel.text = damage.ToString();
            }

            return base.Hit(ref damage);
        }
        public override void Kill(Damage damage)
        {
            StopCoroutine(nameof(ReturnTimer));

            Reset();
        }

#if !UNITY_SERVER || UNITY_EDITOR
        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (reportLabel != null)
            {
                reportLabel.transform.rotation = Camera.main.transform.rotation;
            }
        }
#endif

        private IEnumerator ReturnTimer ()
        {
            yield return new WaitForSeconds(4);

            Reset();
        }
    
#if UNITY_EDITOR
        protected class DummyNetworkCharacter_Editor : NetworkCharacter_Editor
        {
            private SerializedProperty reportLabel;
            private SerializedProperty resetTimer;

            public override void OnEnable()
            {
                base.OnEnable();

                reportLabel ??= serializedObject.FindProperty("reportLabel"); 
                resetTimer ??= serializedObject.FindProperty("resetTimer");
            }
            public override void OnInspectorGUI()
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(reportLabel);
                EditorGUILayout.PropertyField(resetTimer);

                serializedObject.ApplyModifiedProperties();

                EditorGUI.EndChangeCheck();
                
                base.OnInspectorGUI();
            }
        }
#endif
    }
}
