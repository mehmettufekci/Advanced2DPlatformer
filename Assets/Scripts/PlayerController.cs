using GlobalTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    #region public properties
    [Header("Player Properties")]
    public float walkSpeed = 15f;
    public float creepSpeed = 5f;
    public float gravity = 40f;
    public float jumpSpeed = 20f;
    public float doubleJumpSpeed = 15f;
    public float tripleJumpSpeed = 15f;
    public float xWallJumpSpeed = 15f;
    public float yWallJumpSpeed = 15f;
    public float wallRunAmount = 8f;
    public float wallSlideAmount = 0.1f;
    public float glideTime = 2f;
    public float glideDescentAmount = 2f;
    public float powerJumpSpeed = 40f;
    public float powerJumpWaitTime = 1.5f;
    public float dashSpeed = 50f;
    public float dashTime = 0.2f;
    public float dashCooldownTime = 1f;
    public float groundSlamSpeed = 60f;
    public float deadZoneValue = 0.15f;
    public float swimSpeed = 150f;

    //player ability toggles
    [Header("Player Abilities")]
    public bool canDoubleJump;
    public bool canTripleJump;
    public bool canWallJump;
    public bool canJumpAfterWallJump;
    public bool canWallRun;
    public bool canMultipleWallRun;
    public bool canWallSlide;
    public bool canGlide;
    public bool canGlideAfterWallContact;
    public bool canPowerJump;
    public bool canGroundDash;
    public bool canAirDash;
    public bool canGroundSlam;
    public bool canSwim;

    //player state
    [Header("Player State")]
    public bool isJumping;
    public bool isDoubleJumping;
    public bool isTripleJumping;
    public bool isWallJumping;
    public bool isWallRunning;
    public bool isWallSliding;
    public bool isDucking;
    public bool isCreeping;
    public bool isGliding;
    public bool isPowerJumping;
    public bool isDashing; // if necessary add airDashing/groundDashing
    public bool isGroundSlamming;
    public bool isSwimming;
    #endregion

    #region private properties
    //input flags
    private bool _startJump; // to check jump button is pressed
    private bool _releaseJump;
    private bool _holdJump;

    private Vector2 _input;
    private Vector2 _moveDirection;
    private CharacterController2D _characterController;

    private bool _ableToWallRun = true;

    private CapsuleCollider2D _capsuleCollider;
    private Vector2 _originColliderSize;
    //TODO remove later when not needed
    private SpriteRenderer _spriteRenderer;

    private float _currentGlideTime;
    private bool _startGlide = true;

    private float _powerJumpTimer;

    private bool _facingRight;
    private float _dashTimer;

    private float _jumpPadAmount = 15f;
    private float _jumpPadAdjustment = 0f;
    private Vector2 _tempVelocity;

    public Vector2 MoveDirection { get => _moveDirection; }

    #endregion

    void Start()
    {
        _characterController = gameObject.GetComponent<CharacterController2D>();
        _capsuleCollider = gameObject.GetComponent<CapsuleCollider2D>();
        _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        _originColliderSize = _capsuleCollider.size;
    }

    void Update()
    {
        if (_dashTimer > 0)
        {
            _dashTimer -= Time.deltaTime;
        }

        ApplyDeadzones();

        ProcessHorizontalMovement();

        if (_characterController.Below) // On the ground
        {
            OnGround();
        }
        else if (_characterController.InAirEffector)
        {
            InAirEffector();
        }
        else if (_characterController.InWater)
        {
            InWater();
        }
        else // In the air
        {
            InAir();
        }

        _characterController.Move(_moveDirection * Time.deltaTime);
    }

    private void ApplyDeadzones()
    {
        if (_input.x > -deadZoneValue && _input.x < deadZoneValue)
        {
            _input.x = 0f;
        }

        if (_input.y > -deadZoneValue && _input.y < deadZoneValue)
        {
            _input.y = 0f;
        }
    }

    private void ProcessHorizontalMovement()
    {
        if (!isWallJumping)
        {
            _moveDirection.x = _input.x;

            if (_moveDirection.x < 0) // moving to left
            {
                transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                _facingRight = false;
            }
            else if (_moveDirection.x > 0)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                _facingRight = true;
            }

            if (isDashing)
            {
                if (_facingRight)
                {
                    _moveDirection.x = dashSpeed;
                }
                else
                {
                    _moveDirection.x = -dashSpeed;
                }
                _moveDirection.y = 0; // For horizontal dash - if not needed remove
            }
            else if (isCreeping)
            {
                _moveDirection.x *= creepSpeed;
            }
            else
            {
                _moveDirection.x *= walkSpeed;
            }
        }
    }

    void OnGround()
    {
        if (_characterController.AirEffectorType == AirEffectorType.Ladder)
        {
            InAirEffector();
            return;
        }

        if (_characterController.HitGroundThisFrame)
        {
            _tempVelocity = _moveDirection;
        }

        //clear any donwward motion when on ground
        _moveDirection.y = 0f;

        ClearAirAbilityFlags();

        Jump();

        DuckingAndCreeping();

        JumpPad();
    }

    void InWater()
    {
        ClearGroundAbilityFlags();

        AirJump();

        if (_input.y != 0f && canSwim && !_holdJump)
        {
            if (_input.y > 0 && !_characterController.IsSubmerged)
            {
                _moveDirection.y = 0f;
            }
            else
            {
                _moveDirection.y = (_input.y * swimSpeed) * Time.deltaTime;
            }
        }
        else if (_moveDirection.y < 0 && _input.y == 0f)
        {
            _moveDirection.y += 2f;
        }

        if (_characterController.IsSubmerged && canSwim)
        {
            isSwimming = true;
        }
        else
        {
            isSwimming = false;
        }
    }

    void InAirEffector()
    {
        if (_startJump)
        {
            _characterController.DeactivateAirEffector();
            Jump();
        }
        //process movement when on ladder
        if (_characterController.AirEffectorType == AirEffectorType.Ladder)
        {
            if (_input.y > 0f)
            {
                _moveDirection.y = _characterController.AirEffectorSpeed;
            }
            else if (_input.y < 0f)
            {
                _moveDirection.y = -_characterController.AirEffectorSpeed;
            }
            else
            {
                _moveDirection.y = 0f;
            }
        }

        //process movement when in tractor beam
        if (_characterController.AirEffectorType == AirEffectorType.TractorBeam)
        {
            if (_moveDirection.y != 0f)
            {
                _moveDirection.y = Mathf.Lerp(_moveDirection.y, 0f, Time.deltaTime * 4f);
            }
        }

        //process movement when in an updraft
        if (_characterController.AirEffectorType == AirEffectorType.Updraft)
        {
            if (_input.y <= 0f)
            {
                isGliding = false;
            }

            if (isGliding)
            {
                _moveDirection.y = _characterController.AirEffectorSpeed;
            }
            else
            {
                InAir();
            }
        }
    }

    private void JumpPad()
    {
        if (_characterController.GroundType == GroundType.JumpPad)
        {
            _jumpPadAmount = _characterController.JumpPadAmount;

            //if inverted downwards velocity is greater than jump pad amount
            if (-_tempVelocity.y > _jumpPadAmount)
            {
                _moveDirection.y = -_tempVelocity.y * 0.91f;
            }
            else
            {
                _moveDirection.y = _jumpPadAmount;
            }

            //if holding jump button add a little each time we bounce
            if (_holdJump)
            {
                _jumpPadAdjustment += _moveDirection.y * 0.1f;
                _moveDirection.y += _jumpPadAdjustment;
            }
            else
            {
                _jumpPadAdjustment = 0f;
            }

            //impose an upper limit to stop exponential jump height
            if (_moveDirection.y > _characterController.JumpPadUpperLimit)
            {
                _moveDirection.y = _characterController.JumpPadUpperLimit;
            }

        }
    }

    private void DuckingAndCreeping()
    {
        //ducking and creeping
        if (_input.y < 0f)
        {
            if (!isDucking && !isCreeping)
            {
                _capsuleCollider.size = new Vector2(_capsuleCollider.size.x, _capsuleCollider.size.y / 2);
                transform.position = new Vector2(transform.position.x, transform.position.y - (_originColliderSize.y / 4));
                isDucking = true;
                _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp_crouching");
            }

            _powerJumpTimer += Time.deltaTime;
        }
        else
        {
            if (isDucking || isCreeping)
            {
                RaycastHit2D hitCeiling = Physics2D.CapsuleCast(_capsuleCollider.bounds.center, transform.localScale, CapsuleDirection2D.Vertical, 0f, Vector2.up,
                    _originColliderSize.y / 2, _characterController.LayerMask);

                if (!hitCeiling.collider)
                {
                    //return to original size
                    _capsuleCollider.size = _originColliderSize;
                    transform.position = new Vector2(transform.position.x, transform.position.y + (_originColliderSize.y / 4));
                    _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp");
                    isDucking = false;
                    isCreeping = false;
                }
            }

            _powerJumpTimer = 0f;
        }

        if (isDucking && _moveDirection.x != 0)
        {
            isCreeping = true;
            _powerJumpTimer = 0f;
        }
        else
        {
            isCreeping = false;
        }
    }

    private void Jump()
    {
        //Jumping
        if (_startJump)
        {
            _startJump = false;

            if (canPowerJump && isDucking && _characterController.GroundType != GroundType.OneWayPlatform && (_powerJumpTimer > powerJumpWaitTime))
            {
                _moveDirection.y = powerJumpSpeed;
                StartCoroutine("PowerJumpWaiter");
            }
            //check to see if we are on a one way platform
            else if (isDucking && _characterController.GroundType == GroundType.OneWayPlatform)
            {
                StartCoroutine(DisableOneWayPlatform(true));
            }
            else
            {
                _moveDirection.y = jumpSpeed;
            }

            isJumping = true;
            _characterController.DisableGroundCheck();
            _characterController.ClearMovingPlatform();
            _ableToWallRun = true;
        }
    }

    private void ClearAirAbilityFlags()
    {
        //clear flags for in air abilities
        isJumping = false;
        isDoubleJumping = false;
        isTripleJumping = false;
        isWallJumping = false;
        _currentGlideTime = glideTime;
        isGroundSlamming = false;
        _startGlide = true;
        isGliding = false;
    }

    void InAir()
    {
        ClearGroundAbilityFlags();

        AirJump();

        WallRunning();

        GravityCalculations();

        if (isGliding && _input.y <= 0f)
        {
            isGliding = false;
        }

    }

    private void WallRunning()
    {
        //wall running
        if (canWallRun && (_characterController.Left || _characterController.Right))
        {
            if (_characterController.Left && _characterController.LeftWallEffector && !_characterController.LeftIsRunnable)
            {
                return;
            }
            else if (_characterController.Right && _characterController.RightWallEffector && !_characterController.RightIsRunnable)
            {
                return;
            }

            if (_input.y > 0 && _ableToWallRun)
            {
                _moveDirection.y = wallRunAmount;

                if (_characterController.Left)
                {
                    transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                }
                else if (_characterController.Right)
                {
                    transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                }
                StartCoroutine("WallRunWaiter");
            }
        }
        else
        {
            if (canMultipleWallRun)
            {
                StopCoroutine("WallRunWaiter");
                _ableToWallRun = true;
                isWallRunning = false;
            }
        }

        //canGlideAfterWallContact
        if ((_characterController.Left || _characterController.Right) && canWallRun)
        {
            if (canGlideAfterWallContact)
            {
                _currentGlideTime = glideTime;
            }
            else
            {
                _currentGlideTime = 0;
            }
        }
    }

    private void AirJump()
    {
        if (_releaseJump)
        {
            _releaseJump = false;

            if (_moveDirection.y > 0)
            {
                _moveDirection.y *= 0.5f;
            }
        }

        //pressed jump button in air
        if (_startJump)
        {
            //triple jump
            if (canTripleJump && (!_characterController.Left && !_characterController.Right))
            {
                if (isDoubleJumping && !isTripleJumping)
                {
                    _moveDirection.y = tripleJumpSpeed;
                    isTripleJumping = true;
                }
            }

            //double jump
            if (canDoubleJump && (!_characterController.Left && !_characterController.Right))
            {
                if (!isDoubleJumping)
                {
                    _moveDirection.y = doubleJumpSpeed;
                    isDoubleJumping = true;
                }
            }

            //jump in water
            if (_characterController.InWater)
            {
                isDoubleJumping = false;
                isTripleJumping = false;
                _moveDirection.y = jumpSpeed;
            }

            //wall jump
            if (canWallJump && (_characterController.Left || _characterController.Right))
            {
                if (_characterController.Left && _characterController.LeftWallEffector && !_characterController.LeftIsJumpable)
                {
                    return;
                }
                else if (_characterController.Right && _characterController.RightWallEffector && !_characterController.RightIsJumpable)
                {
                    return;
                }

                if (_moveDirection.x <= 0 && _characterController.Left)
                {
                    _moveDirection.x = xWallJumpSpeed;
                    _moveDirection.y = yWallJumpSpeed;
                    transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                }
                else if (_moveDirection.x >= 0 && _characterController.Right)
                {
                    _moveDirection.x = -xWallJumpSpeed;
                    _moveDirection.y = yWallJumpSpeed;
                    transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                }

                //isWallJumping = true;
                StartCoroutine("WallJumpWaiter");

                if (canJumpAfterWallJump)
                {
                    isDoubleJumping = false;
                    isTripleJumping = false;
                }
            }

            _startJump = false;
        }
    }

    private void ClearGroundAbilityFlags()
    {
        if ((isDucking || isCreeping) && _moveDirection.y > 0)
        {
            StartCoroutine("ClearDuckingState");
        }
        //clear powerJumpTimer
        _powerJumpTimer = 0f;
    }

    void GravityCalculations()
    {
        //detects if something above player
        if (_moveDirection.y > 0f && _characterController.Above)
        {
            if (_characterController.CeilingType == GroundType.OneWayPlatform)
            {
                StartCoroutine(DisableOneWayPlatform(false));
            }
            else
            {
                _moveDirection.y = 0f;
            }
        }

        //apply wall slide adjustment
        if (canWallSlide && (_characterController.Left || _characterController.Right))
        {
            if (_characterController.HitWallThisFrame)
            {
                _moveDirection.y = 0f;
            }

            if (_moveDirection.y <= 0)
            {
                if (_characterController.Left && _characterController.LeftWallEffector)
                {
                    _moveDirection.y -= (gravity * _characterController.LeftSlideModifier) * Time.deltaTime;
                }
                else if (_characterController.Right && _characterController.RightWallEffector)
                {
                    _moveDirection.y -= (gravity * _characterController.RightSlideModifier) * Time.deltaTime;
                }
                else
                {
                    _moveDirection.y -= (gravity * wallSlideAmount) * Time.deltaTime;
                }
            }
            else
            {
                _moveDirection.y -= gravity * Time.deltaTime;
            }
        }
        else if (canGlide && _input.y > 0f && _moveDirection.y < 0.2f)  // glide adjustment
        {
            if (_currentGlideTime > 0f)
            {
                isGliding = true;

                if (_startGlide)
                {
                    _moveDirection.y = 0;
                    _startGlide = false;
                }

                _moveDirection.y -= glideDescentAmount * Time.deltaTime;
                _currentGlideTime -= Time.deltaTime;
            }
            else
            {
                isGliding = false;
                _moveDirection.y -= gravity * Time.deltaTime;
            }
        }
        //else if (canGroundSlam && !isPowerJumping && _input.y < 0f && _moveDirection.y < 0f) // ground slam
        else if (isGroundSlamming && !isPowerJumping && _moveDirection.y < 0f)
        {
            _moveDirection.y = -groundSlamSpeed;
        }
        else if (!isDashing)
        {
            //Normal gravity
            _moveDirection.y -= gravity * Time.deltaTime;
        }
    }

    #region Input Methods
    public void OnMovement(InputAction.CallbackContext context)
    {
        _input = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _startJump = true;
            _releaseJump = false; // Havada jump butona basýldýðýnda tekrar zýplamasýn diye
            _holdJump = true;
        }
        else if (context.canceled)
        {
            _releaseJump = true;
            _startJump = false; // Havada jump butona basýldýðýnda tekrar zýplamasýn diye
            _holdJump = false;
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started && _dashTimer <= 0)
        {
            if ((canAirDash && !_characterController.Below) || (canGroundDash && _characterController.Below))
            {
                StartCoroutine("Dash");
            }
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && _input.y < 0f)
        {
            if (canGroundSlam)
            {
                isGroundSlamming = true;
            }
        }
    }
    #endregion

    #region coroutines
    IEnumerator WallJumpWaiter()
    {
        isWallJumping = true;
        yield return new WaitForSeconds(0.4f);
        isWallJumping = false;
    }

    IEnumerator WallRunWaiter()
    {
        isWallRunning = true;
        yield return new WaitForSeconds(0.5f);
        isWallRunning = false;
        if (!isWallJumping)
        {
            _ableToWallRun = false;
        }
    }

    IEnumerator ClearDuckingState()
    {
        yield return new WaitForSeconds(0.05f);

        RaycastHit2D hitCeiling = Physics2D.CapsuleCast(_capsuleCollider.bounds.center, transform.localScale, CapsuleDirection2D.Vertical, 0f, Vector2.up,
                       _originColliderSize.y / 2, _characterController.LayerMask);

        if (!hitCeiling.collider)
        {
            //return to original size
            _capsuleCollider.size = _originColliderSize;
            //transform.position = new Vector2(transform.position.x, transform.position.y + (_originColliderSize.y / 4));
            _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp");
            isDucking = false;
            isCreeping = false;
        }
    }

    IEnumerator PowerJumpWaiter()
    {
        isPowerJumping = true;
        yield return new WaitForSeconds(0.8f);
        isPowerJumping = false;
    }

    IEnumerator Dash()
    {
        isDashing = true;
        yield return new WaitForSeconds(dashTime);
        isDashing = false;
        _dashTimer = dashCooldownTime;
    }

    IEnumerator DisableOneWayPlatform(bool checkBelow)
    {
        bool originalCanGroundSlam = canGroundSlam;
        GameObject tempOneWayPlatform = null;

        if (checkBelow)
        {
            Vector2 raycastBelow = transform.position - new Vector3(0, _capsuleCollider.size.y * 0.5f, 0);
            RaycastHit2D hit = Physics2D.Raycast(raycastBelow, Vector2.down, _characterController.RaycastDistance, _characterController.LayerMask);
            if (hit.collider)
            {
                tempOneWayPlatform = hit.collider.gameObject;
            }
        }
        else
        {
            Vector2 raycastAbove = transform.position + new Vector3(0, _capsuleCollider.size.y * 0.5f, 0);
            RaycastHit2D hit = Physics2D.Raycast(raycastAbove, Vector2.up, _characterController.RaycastDistance, _characterController.LayerMask);
            if (hit.collider)
            {
                tempOneWayPlatform = hit.collider.gameObject;
            }
        }

        if (tempOneWayPlatform)
        {
            tempOneWayPlatform.GetComponent<EdgeCollider2D>().enabled = false;
            canGroundSlam = false;
        }

        yield return new WaitForSeconds(0.25f);

        if (tempOneWayPlatform)
        {
            tempOneWayPlatform.GetComponent<EdgeCollider2D>().enabled = true;
            canGroundSlam = originalCanGroundSlam;
        }
    }

    #endregion
}
