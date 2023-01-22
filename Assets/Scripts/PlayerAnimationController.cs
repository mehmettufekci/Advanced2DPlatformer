using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    PlayerController playerController;
    CharacterController2D characterController;
    Animator animator;

    void Start()
    {
        playerController = gameObject.GetComponent<PlayerController>();
        characterController = gameObject.GetComponent<CharacterController2D>();
        animator = gameObject.GetComponentInChildren<Animator>();
    }

    void Update()
    {
        animator.SetFloat("horizontalMovement", Mathf.Abs(playerController.MoveDirection.x));
        animator.SetFloat("verticalMovement", Mathf.Abs(playerController.MoveDirection.y));

        if (characterController.Below)
        {
            animator.SetBool("isGrounded", true);
        }
        else
        {
            animator.SetBool("isGrounded", false);
        }

        if ((characterController.Left || characterController.Right) && !characterController.Below)
        {
            animator.SetBool("onWall", true);
        }
        else
        {
            animator.SetBool("onWall", false);
        }

        if (playerController.isGliding)
        {
            animator.SetBool("isGliding", true);
        }
        else
        {
            animator.SetBool("isGliding", false);
        }

        if (playerController.isDucking)
        {
            animator.SetBool("isCrouuching", true);
        }
        else
        {
            animator.SetBool("isCrouuching", false);
        }

        if (characterController.IsSubmerged)
        {
            animator.SetBool("inWater", true);
        }
        else
        {
            animator.SetBool("inWater", false);
        }
    }
}
