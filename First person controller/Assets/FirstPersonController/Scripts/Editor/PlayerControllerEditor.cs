using Cinemachine;
using Cinemachine.Editor;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEditorInternal;
using UnityEngine;
using static PlayerController;
using static UnityEngine.UI.Image;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    //Camera settings
    SerializedProperty lookSpeed;
    SerializedProperty cameraAngleLimits;
    //SerializedProperty cameraOffset;
    SerializedProperty cameraFovCurve;
    SerializedProperty rotateWithMovingPlatforms;
    SerializedProperty useHeadBobCurves;

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

    //audio
    SerializedProperty audio;
    SerializedProperty enableDefaultSounds;

    //Foldout groups
    bool cameraSettingsDD = false;
    bool movmentSettingsDD = false;
    bool jumpSettingsDD = false;
    bool gravitySettingsDD = false;
    bool groundChecksDD = false;
    bool objectAssignmentDD = false;
    bool AudioSettingsDD = false;



    private void OnEnable() {
        //Camera settings
        lookSpeed = serializedObject.FindProperty("lookSpeed");
        cameraAngleLimits = serializedObject.FindProperty("cameraAngleLimits");
        //cameraOffset = serializedObject.FindProperty("cameraOffset");
        cameraFovCurve = serializedObject.FindProperty("cameraFovCurve");
        rotateWithMovingPlatforms = serializedObject.FindProperty("rotateWithMovingPlatforms");
        useHeadBobCurves = serializedObject.FindProperty("useHeadBobCurves");


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

        audio = serializedObject.FindProperty("footstepAudioClips");
        enableDefaultSounds = serializedObject.FindProperty("enableDefaultSounds");


    }

    public override void OnInspectorGUI() {
        //Base inspectotor 
        base.OnInspectorGUI();  
        EditorGUILayout.Space(20);

        serializedObject.Update();

        PlayerController controller = (PlayerController)target;
        Undo.RecordObject(controller, ("Changed player controller variable"));

        # region Camera settings
        cameraSettingsDD = EditorGUILayout.BeginFoldoutHeaderGroup(cameraSettingsDD, "Camera settings");
        if(cameraSettingsDD) {
            controller.lookSpeed = EditorGUILayout.FloatField(new GUIContent("Look speed", "The speed at which the players camera rotates."), controller.lookSpeed);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Camera angle limits", "The minimum and maxamum angle that the players camera can look up or down."));
                GUILayout.Label("Min");
                controller.cameraAngleLimits.x = EditorGUILayout.FloatField(controller.cameraAngleLimits.x);
                GUILayout.Label("Max");
                controller.cameraAngleLimits.y = EditorGUILayout.FloatField(controller.cameraAngleLimits.y);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
                controller.cameraFOVCurve = EditorGUILayout.CurveField(new GUIContent("Camera F.O.V. curve", "The 'field of view' of the players camera at certain speeds, The 'X' axis being the player speed and the 'Y' axis being the FOV at the point."), controller.cameraFOVCurve);
                GUILayout.Label(new GUIContent("Adjustment speed", "The speed that the FOV will adjust to the new target FOV for smoother transitions."));
                controller.cameraFOVAdjustSpeed = EditorGUILayout.FloatField(controller.cameraFOVAdjustSpeed);
            GUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(rotateWithMovingPlatforms, new GUIContent("Rotate with moving platform", "If the players camera should rotate with the moving platform they are standing on."));

            EditorGUILayout.Space(15f);

            EditorGUILayout.PropertyField(useHeadBobCurves, new GUIContent("Use head bob curves", "Whether or not the head bob should use curves as its source for the frequency and amplitude"));
            if (controller.useHeadBobCurves) {
                controller.headBobFrequencyCurve = EditorGUILayout.CurveField(new GUIContent("Head bob frequency", "The speed at which the camera will oscillate. The 'X' axis being the speed of the player and the 'Y' axis being the frequency."), controller.headBobFrequencyCurve);
                controller.headBobAmplitudeCurve = EditorGUILayout.CurveField(new GUIContent("Head bob amplitude", "The strenght at which the camera will oscillate. The 'X' axis being the speed of the player and the 'Y' axis being the amplitude."), controller.headBobAmplitudeCurve);
            }
            else {
                controller.headBobFrequency = EditorGUILayout.FloatField(new GUIContent("Head bob frequency", "The speed at which the camera will oscillate."), controller.headBobFrequency);
                controller.headBobAmplitude = EditorGUILayout.FloatField(new GUIContent("Head bob amplitude", "The strenght at which the camera will oscillate."), controller.headBobAmplitude);
            }




            EditorGUILayout.Space(15f);

            //Camera tracking settings goes here//
            controller.cameraStyle = (PlayerController.cameraStyles)EditorGUILayout.EnumPopup(new GUIContent("Camera style", "When set to standard the player model will rotate to the direction of movement while only using 1 dimension of the animation graph (Forward and idle), and when set to locked the player will maintain the ditrection they are looking and use the full set of animatons."), controller.cameraStyle);

            controller.TPRotationSpeed = EditorGUILayout.FloatField(new GUIContent("Character rotation speed", "The speed that the player model will rotate to the new rotation."), controller.TPRotationSpeed);

            controller.cameraTarget = EditorGUILayout.ObjectField(new GUIContent("Camera Target", "If left as 'null' then nothing will happen but if there is a selected transform then the camera will remain focused on the object."), controller.cameraTarget, typeof(Transform), true) as Transform;

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

            EditorGUILayout.Space(15);


            controller.maxStepHeight = EditorGUILayout.FloatField("Max step height", controller.maxStepHeight);
            controller.minStepDepth = EditorGUILayout.FloatField("Min step depth", controller.minStepDepth);
            controller.stepSmoothing = EditorGUILayout.FloatField("Step smooting", controller.stepSmoothing);
            controller.maxStepAngle = EditorGUILayout.FloatField("Max step angle", controller.maxStepAngle);


            EditorGUILayout.Space(20);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        #endregion

        #region Jump settings
        jumpSettingsDD = EditorGUILayout.BeginFoldoutHeaderGroup(jumpSettingsDD, "Jump settings");
        if(jumpSettingsDD) {
            controller.jumpMode = (PlayerController.jumpModes)EditorGUILayout.EnumPopup(new GUIContent("Jump mode", "Changes the style of jumping between various modes."), controller.jumpMode);

            switch (controller.jumpMode) {
                case PlayerController.jumpModes.Standard:
                    EditorGUILayout.PropertyField(jumpHeight, new GUIContent("Jump height", "This is the height that the player will jump. Note that due to the way the physics system works within unity the player will always reach just under this height."));
                    break;

                case PlayerController.jumpModes.Charge:
                    controller.chargeCurve = EditorGUILayout.CurveField(new GUIContent("Jump charge curve", "The height that the player will jump too depending on how long they hold the jump button. The 'X' axis represents the time and the 'Y' axis represents the height."), controller.chargeCurve);
                    break;

                case PlayerController.jumpModes.Hold:
                    break;
            }

            EditorGUILayout.PropertyField(maxJumps, new GUIContent("Max jumps", "If this number is more than one the player will be able to complete that amount of jumps before landing again. If this value is set to 1 then it will enable 'cyote time'."));
            
            if(controller.maxJumps == 1) controller.coyoteTime = EditorGUILayout.FloatField(new GUIContent("Coyote time", "The amount of time after the player falls off a ledge where they can still jump. Note this is only enabled when the 'Max jumps' is set to 1."), controller.coyoteTime);

            controller.jumpBufferMode = (PlayerController.jumpBufferModes)EditorGUILayout.EnumPopup(new GUIContent("Jump buffer", "When jump buffer is enabled, the player will be able to press the jump button before landing and it will cache the jump to be used straight away after landing."), controller.jumpBufferMode);
            
            if (controller.jumpBufferMode == jumpBufferModes.Single) {
                controller.maxJumpBuffer = EditorGUILayout.FloatField(new GUIContent("Max jump buffer", "The maximum amount of time before landing where a jump will be added to the buffer."), controller.maxJumpBuffer);

            }

            EditorGUILayout.Space(20);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        #endregion

        #region Gravity settings
        gravitySettingsDD = EditorGUILayout.BeginFoldoutHeaderGroup(gravitySettingsDD, "Gravity settings");
        if (gravitySettingsDD) {      
            

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

        #region Ground checks
        groundChecksDD = EditorGUILayout.BeginFoldoutHeaderGroup(groundChecksDD, "Ground checks");
        if (groundChecksDD) {
            controller.groundCheckOrigin = EditorGUILayout.Vector3Field("Ground check origin", controller.groundCheckOrigin);
            EditorGUILayout.PropertyField(groundCheckDistance);

            EditorGUILayout.Space(15f);

            controller.groundAngleCheckOrigin = EditorGUILayout.Vector3Field("Ground angle check origin", controller.groundAngleCheckOrigin);
            EditorGUILayout.PropertyField(groundAngleCheckDistance);

            EditorGUILayout.Space(15f);

            LayerMask groundMask = EditorGUILayout.MaskField("Ground layer", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(controller.groundLayer), InternalEditorUtility.layers);
            controller.groundLayer = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(groundMask);

            LayerMask dynamicGravityMask = EditorGUILayout.MaskField("Dynamic gravity layer", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(controller.dynamicGravityLayer), InternalEditorUtility.layers);
            controller.dynamicGravityLayer = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(dynamicGravityMask);

            EditorGUILayout.Space(20);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        #endregion

        #region Audio settings
        AudioSettingsDD = EditorGUILayout.BeginFoldoutHeaderGroup(AudioSettingsDD, "Audio settings");
        EditorGUILayout.EndFoldoutHeaderGroup();

        if(AudioSettingsDD) {
            controller.walkStepTime = EditorGUILayout.FloatField(new GUIContent("Walk step time", "The amount of time between each sound footstep at walking speed. The frequecy on the footsteps will increase as the players speed increases."), controller.walkStepTime);
            EditorGUILayout.PropertyField(enableDefaultSounds, new GUIContent("Enable default sounds", "If the player is standing on an object with no tag match, The first set os sounds in the list will be used."));

            EditorGUILayout.PropertyField(audio);

            EditorGUILayout.Space(20);
        }
        #endregion


        #region Object assignment
        objectAssignmentDD = EditorGUILayout.BeginFoldoutHeaderGroup(objectAssignmentDD, "Object assignment");
        if (objectAssignmentDD) {
            //controller.test = EditorGUILayout.ObjectField("Camera Target", controller.test, typeof(Transform), true) as Transform;
            controller.playerObject = EditorGUILayout.ObjectField("Player object", controller.playerObject, typeof(Transform), true) as Transform;
            //controller.playerCamera = EditorGUILayout.ObjectField("Player camera", controller.playerCamera, typeof(CinemachineVirtualCamera), true) as CinemachineVirtualCamera;
            controller.cameraPivot = EditorGUILayout.ObjectField("Player camera", controller.cameraPivot, typeof(GameObject), true) as GameObject;


            EditorGUILayout.Space(20);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        #endregion

        serializedObject.ApplyModifiedProperties();
    }
}



