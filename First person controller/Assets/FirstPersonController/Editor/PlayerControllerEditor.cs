using Cinemachine.Editor;
using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    //Camera settings
    SerializedProperty lookSpeed;
    SerializedProperty cameraAngleLimits;
    //SerializedProperty cameraOffset;
    SerializedProperty cameraFovCurve;


    //Movement settings
    SerializedProperty walkSpeed;
    SerializedProperty sprintSpeed;
    SerializedProperty acceleration;
    SerializedProperty playerDrag;

    //Jump settings
    SerializedProperty jumpHeight;
    SerializedProperty maxJumps;

    //Gravity settings
    SerializedProperty groundCheckOrigin;
    SerializedProperty groundCheckDistance;
    SerializedProperty groundAngleCheckOrigin;
    SerializedProperty groundAngleCheckDistance;

    SerializedProperty attractor;




    //Foldout groups
    bool cameraSettingsDD = false;
    bool movmentSettingsDD = false;
    bool jumpSettingsDD = false;
    bool gravitySettings = false;


    private void OnEnable() {
        //Camera settings
        lookSpeed = serializedObject.FindProperty("lookSpeed");
        cameraAngleLimits = serializedObject.FindProperty("cameraAngleLimits");
        //cameraOffset = serializedObject.FindProperty("cameraOffset");
        cameraFovCurve = serializedObject.FindProperty("cameraFovCurve");

        //Walk settings
        walkSpeed = serializedObject.FindProperty("walkSpeed");
        sprintSpeed = serializedObject.FindProperty("sprintSpeed");
        acceleration = serializedObject.FindProperty("acceleration");
        playerDrag = serializedObject.FindProperty("playerDrag");

        //Jump setttings
        jumpHeight = serializedObject.FindProperty("jumpHeight");
        maxJumps = serializedObject.FindProperty("maxJumps");

        //Gravity setttings
        groundCheckOrigin = serializedObject.FindProperty("groundCheckOrigin");
        groundCheckDistance = serializedObject.FindProperty("groundCheckDistance");
        groundAngleCheckOrigin = serializedObject.FindProperty("groundAngleCheckOrigin");
        groundAngleCheckDistance = serializedObject.FindProperty("groundAngleCheckDistance");

        attractor = serializedObject.FindProperty("attractor");

    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        
        EditorGUILayout.Space(20);

        serializedObject.Update();

        PlayerController controller = (PlayerController)target;
        Undo.RecordObject(controller, ("Changed player controller variable"));

        # region Camera settings
        cameraSettingsDD = EditorGUILayout.BeginFoldoutHeaderGroup(cameraSettingsDD, "Camera settings");
        if(cameraSettingsDD) {
            EditorGUILayout.PropertyField(lookSpeed);

            GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Camera angle limits");
                GUILayout.Label("Min");
                controller.cameraAngleLimits.x = EditorGUILayout.FloatField(controller.cameraAngleLimits.x);
                GUILayout.Label("Max");
                controller.cameraAngleLimits.y = EditorGUILayout.FloatField(controller.cameraAngleLimits.y);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
                controller.cameraFOVCurve = EditorGUILayout.CurveField("Camera F.O.V. curve", controller.cameraFOVCurve);
                GUILayout.Label("Adjustment speed");
                controller.cameraFOVAdjustSpeed = EditorGUILayout.FloatField(controller.cameraFOVAdjustSpeed);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(15f);

            //Camera tracking settings goes here//
            controller.cameraStyle = (PlayerController.cameraStyles)EditorGUILayout.EnumPopup("Camera style", controller.cameraStyle);

            switch (controller.cameraStyle) {
                case PlayerController.cameraStyles.Standard:
                    controller.TPRotationSpeed = EditorGUILayout.FloatField("Character rotation speed", controller.TPRotationSpeed);
                    break;

                case PlayerController.cameraStyles.Locked:
                    break;

                case PlayerController.cameraStyles.Focused:
                    //controller.cameraTarget = EditorGUILayout.ObjectField("Camera Target", controller.cameraTarget, typeof(Transform)) as Transform;
                    break;              
            }

            EditorGUILayout.Space(20);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        #endregion

        #region Movement settings
        movmentSettingsDD = EditorGUILayout.BeginFoldoutHeaderGroup(movmentSettingsDD, "Movment settings"); 
        if (movmentSettingsDD) {
            GUILayout.BeginHorizontal();
                GUILayout.Label("Crouch speed");
                controller.walkSpeed = EditorGUILayout.FloatField(controller.walkSpeed);
                GUILayout.Label("Walk speed");
                controller.walkSpeed = EditorGUILayout.FloatField(controller.walkSpeed);
                GUILayout.Label("Sprint speed");
                controller.sprintSpeed = EditorGUILayout.FloatField(controller.sprintSpeed);
            GUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(acceleration);
            EditorGUILayout.PropertyField(playerDrag);

            EditorGUILayout.Space(20);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        #endregion

        #region Jump settings
        jumpSettingsDD = EditorGUILayout.BeginFoldoutHeaderGroup(jumpSettingsDD, "Jump settings");
        if(jumpSettingsDD) {
            EditorGUILayout.PropertyField(jumpHeight);
            EditorGUILayout.PropertyField(maxJumps);

            EditorGUILayout.Space(20);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        #endregion

        #region Gravity settings
        gravitySettings = EditorGUILayout.BeginFoldoutHeaderGroup(gravitySettings, "Gravity settings");
        if (gravitySettings) {      
            controller.groundCheckOrigin = EditorGUILayout.Vector3Field("Ground check origin", controller.groundCheckOrigin);
            EditorGUILayout.PropertyField(groundCheckDistance);

            EditorGUILayout.Space(15f);

            controller.groundAngleCheckOrigin = EditorGUILayout.Vector3Field("Ground angle check origin", controller.groundAngleCheckOrigin);
            EditorGUILayout.PropertyField(groundAngleCheckDistance);

            EditorGUILayout.Space(15f);

            LayerMask layorMask = EditorGUILayout.MaskField("Ground layer", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(controller.groundMask), InternalEditorUtility.layers);
            controller.groundMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(layorMask);

            GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Gravity adjustment limits");
                GUILayout.Label("Min");
                controller.maxGravityChange.x = EditorGUILayout.FloatField(controller.maxGravityChange.x);
                GUILayout.Label("Max");
                controller.maxGravityChange.y = EditorGUILayout.FloatField(controller.maxGravityChange.y);
            GUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(attractor);

            EditorGUILayout.Space(20);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        #endregion

        serializedObject.ApplyModifiedProperties();
    }
}
