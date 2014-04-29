﻿/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary and  confidential.  Not for distribution.            *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement between *
* Leap Motion and you, your company or other organization.                     *
* Author: Matt Tytel
\******************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using Leap;

public class HandController : MonoBehaviour {

  // Reference distance from thumb base to pinky base in mm.
  protected const float MODEL_PALM_DIAMETER = 85.0f;

  public HandModel leftGraphicsModel;
  public HandModel leftPhysicsModel;
  public HandModel rightGraphicsModel;
  public HandModel rightPhysicsModel;

  private Controller leap_controller_;
  private Dictionary<int, HandModel> hands_;
  private Dictionary<int, HandModel> physics_hands_;

  void Start () {
    leap_controller_ = new Controller();
    hands_ = new Dictionary<int, HandModel>();
    physics_hands_ = new Dictionary<int, HandModel>();
  }

  private void IgnoreHandCollisions(HandModel hand) {
    // Ignores hand collisions with immovable objects.
    Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
    Collider[] hand_colliders = hand.GetComponentsInChildren<Collider>();

    for (int i = 0; i < colliders.Length; ++i) {
      for (int h = 0; h < hand_colliders.Length; ++h) {
        if (colliders[i].rigidbody == null)
          Physics.IgnoreCollision(colliders[i], hand_colliders[h]);
      }
    }
  }

  HandModel CreateHand(HandModel model) {
    HandModel hand_model = Instantiate(model, transform.position, transform.rotation)
                           as HandModel;
    IgnoreHandCollisions(hand_model);
    return hand_model;
  }

  private float GetPalmDiameter(Hand hand) {
    Finger thumb = hand.Fingers[(int)Finger.FingerType.TYPE_THUMB];
    Finger pinky = hand.Fingers[(int)Finger.FingerType.TYPE_PINKY];
    Vector thumb_base = thumb.JointPosition(Finger.FingerJoint.JOINT_MCP);
    Vector pinky_base = pinky.JointPosition(Finger.FingerJoint.JOINT_MCP);

    return (thumb_base - pinky_base).Magnitude;
  }

  private void UpdateModels(Dictionary<int, HandModel> all_hands, HandList leap_hands,
                            HandModel left_model, HandModel right_model) {
    List<int> ids_to_check = new List<int>(all_hands.Keys);

    // Go through all the active hands and update them.
    int num_hands = leap_hands.Count;
    for (int h = 0; h < num_hands; ++h) {
      Hand leap_hand = leap_hands[h];
      
      // Only create or update if the hand is enabled.
      if ((leap_hand.IsLeft && left_model != null) ||
          (leap_hand.IsRight && right_model != null)) {

        ids_to_check.Remove(leap_hand.Id);

        // Create the hand and initialized it if it doesn't exist yet.
        if (!all_hands.ContainsKey(leap_hand.Id)) {
          HandModel model = leap_hand.IsLeft? left_model : right_model;
          HandModel new_hand = CreateHand(model);
          new_hand.SetLeapHand(leap_hand);
          new_hand.InitHand(transform);
          all_hands[leap_hand.Id] = new_hand;
        }

        // Make sure we update the Leap Hand reference.
        HandModel hand_model = all_hands[leap_hand.Id];
        hand_model.SetLeapHand(leap_hand);

        // Set scaling based on reference hand.
        float hand_scale = GetPalmDiameter(leap_hand) / MODEL_PALM_DIAMETER;
        hand_model.transform.localScale = hand_scale * transform.localScale;

        hand_model.UpdateHand(transform);
      }
    }

    // Destroy all hands with defunct IDs.
    for (int i = 0; i < ids_to_check.Count; ++i) {
      Destroy(all_hands[ids_to_check[i]].gameObject);
      all_hands.Remove(ids_to_check[i]);
    }
  }

  void Update() {
    Frame frame = leap_controller_.Frame();
    UpdateModels(hands_, frame.Hands, leftGraphicsModel, rightGraphicsModel);
  }

  void FixedUpdate () {
    Frame frame = leap_controller_.Frame();
    UpdateModels(physics_hands_, frame.Hands, leftPhysicsModel, rightPhysicsModel);
  }
}
