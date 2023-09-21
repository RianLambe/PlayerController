using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class ExampleSceneManager : MonoBehaviour
{
    [SerializeField] List<GameObject> exampleCharacters;
    CompleteCharacterController CurrentCharacterController;
    int currentCharacterIndex;
    public Transform planet;
    public TMP_Text speedText;

    private void Awake() {
        CurrentCharacterController = GameObject.FindObjectOfType<CompleteCharacterController>();
    }

    //Cycles the character through the list
    void OnChangeCharacter() {
        foreach (CompleteCharacterController OldCharacter in FindObjectsOfType(typeof(CompleteCharacterController))) {
            Destroy(OldCharacter.transform.gameObject);
        }

        currentCharacterIndex = (currentCharacterIndex + 1) % exampleCharacters.Count;
        CurrentCharacterController = Instantiate(exampleCharacters[currentCharacterIndex], transform.position, transform.rotation).GetComponent<CompleteCharacterController>();
    }

    //Sets the characters attractor variable to the planet
    void OnPlanet() {
        CurrentCharacterController.attractor = planet;
    }

    private void FixedUpdate() {
        speedText.text = "Speed : " + CurrentCharacterController.rb.velocity.magnitude.ToString("F2");
    }
}
