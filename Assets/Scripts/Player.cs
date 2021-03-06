﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;


public class Player : MonoBehaviour
{
    [Header("Misc")]
    [SerializeField] GameObject projectile = null;
    [SerializeField] List<Transform> firePoints = null;
    [SerializeField] GameObject lightSource = null;
    [SerializeField] float smooth = 5f;

    [Header("Player Stats")]
    [SerializeField] float health = 200f;
    [SerializeField] float baseMoveSpeed = 1f;
    [SerializeField] float speedMultiplier = 5f;
    [SerializeField] GameObject deathVFX = null;
    [SerializeField] AudioClip bowSFX = null;
    [SerializeField] AudioClip deathSFX = null;
    [SerializeField] List<AudioClip> hurtSFX = null;
    [SerializeField] float durationOfDeath = 3f;
    [Range(0, 1)] [SerializeField] float hurtVolume = 1f;
    private int facing = -1;

    [Header("Sprite Blink")]
    [SerializeField] float spriteBlinkMiniDuration = 0.1f;
    [SerializeField] float spriteBlinkTotalDuration = 1.0f;
    private float spriteBlinkTimer = 0.0f;
    private float spriteBlinkTotalTimer = 0.0f;

    [Header("Dash")]
    [SerializeField] float dashTimer = 0.25f;
    [SerializeField] float dashCooldown = 3f;
    [SerializeField] float dashSpeed = 10f;
    [SerializeField] float dashing = 0f;

    Rigidbody2D rb;
    Animator anim;
    AudioSource aud;
    SpriteRenderer sp;
    SwitchController sc;
    private bool alive = true;
    private bool aiming = false;
    private bool playerHit = false;
    public bool playerActive = false;
    private bool shadow = false;
    private bool shadowDeath = false;
    private float updatedMoveSpeed, rotation, angle;

    Vector2 movement, savedVelocity;    // stores x (horiz) and y (vert)
    Vector2 mousePos, lookDir;                   // stores mouse position in relation to camera

    public DashState dashState;
    public Facing faceState;

    public enum DashState
    {
        Ready,
        Dashing,
        Cooldown
    }

    public enum Facing
    {
        Left,
        Right,
        Down,
        Up
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        aud = FindObjectOfType<AudioSource>();
        sp = GetComponent<SpriteRenderer>();
        sc = FindObjectOfType<SwitchController>();

        if (gameObject.name.Contains("Shadow"))
            shadow = true;
        else if (gameObject.name.Contains("Human"))
            shadow = false;

        anim.SetFloat("Horizontal", 1);
        updatedMoveSpeed = baseMoveSpeed;
    }

    void Update()
    {
        Animate();
        LightRotation();
        if (playerActive)
        {
            Move();

            if (Input.GetKeyDown(KeyCode.LeftShift))
                sc.SwitchPlayer();

            if (!shadow)
            {
                //ReadyFire();
                //Fire();
            }
        }
        else if (!playerActive)
        {
            StopMovement();
        }

        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (playerHit)
            GracePeriod();
    }

    // Movement
    void FixedUpdate()
    {
        // Movement
        if (dashState != DashState.Dashing)
            Movement();


        if (shadow && playerActive)
            Dash();
    }

    public void SetActive(bool active)
    {
        playerActive = active;
    }

    private void Move()
    {
        // Movement inputs tied to animation
        movement.x = Input.GetAxisRaw("Horizontal");        // value btwn -1 and 1
        movement.y = Input.GetAxisRaw("Vertical");          // works default with WASD and arrow keys

        if (aiming || Input.GetButton("Fire2") && !shadow)
            updatedMoveSpeed = baseMoveSpeed * 0f;
        else
            updatedMoveSpeed = baseMoveSpeed;

        anim.SetFloat("Base Speed", updatedMoveSpeed);
        anim.SetFloat("Speed", movement.sqrMagnitude);      // sqrMag will always be pos, optimal since sqr root is unneeded 
    }

    private void Animate()
    {
        if (movement.y >= 1)
        {
            // facing/moving UP
            if (aiming && facing >= 0)
            {
                facing = 0;
            }

            faceState = Facing.Up;
            anim.SetFloat("Vertical", 1);
            anim.SetFloat("Horizontal", 0);
        }
        else if (movement.y < 0)
        {
            // facing/moving DOWN
            if (aiming && facing >= 0)
            {
                facing = 1;
            }

            faceState = Facing.Down;
            anim.SetFloat("Vertical", -1);
            anim.SetFloat("Horizontal", 0);
        }

        if (movement.x >= 1)
        {
            // facing/moving RIGHT
            if (aiming && facing >= 0)
            {
                facing = 2;
            }

            faceState = Facing.Right;
            anim.SetFloat("Horizontal", 1);
            anim.SetFloat("Vertical", 0);
        }
        else if (movement.x < 0)
        {
            // facing/moving LEFT
            if (aiming && facing >= 0)
            {
                facing = 3;
            }

            faceState = Facing.Left;
            anim.SetFloat("Horizontal", -1);
            anim.SetFloat("Vertical", 0);
        }
    }

    private void StopMovement()
    {
        //Debug.Log("Stopping movement for: " + gameObject.name);
        //rb.velocity = new Vector3(0f, 0f, 0f);
    }

    private void ReadyFire()
    {
        if (alive && !shadow)
        {
            if (Input.GetButton("Fire2") && aiming == false)
            {
                aiming = true;
                anim.SetBool("Shoot", true);
            }
            else if (Input.GetButtonUp("Fire2"))
            {
                facing = -1;
                aiming = false;
                anim.SetBool("Shoot", false);
            }
        }
    }

    public void SetRotation(int rotation)
    {
        facing = rotation;
    }

    private void LightRotation()
    {
        /*
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 lookDir = mousePos - rb.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        Quaternion target = Quaternion.Euler(0, 0, angle);
        */
        Quaternion target = Quaternion.identity;

        if (faceState == Facing.Down)
        {
            target = Quaternion.Euler(0, 0, 180);
        }
        else if (faceState == Facing.Up)
        {
            target = Quaternion.Euler(0, 0, 0);
        }
        else if (faceState == Facing.Right)
        {
            target = Quaternion.Euler(0, 0, -90);
        }
        else if (faceState == Facing.Left)
        {
            target = Quaternion.Euler(0, 0, 90);
        }

        if (lightSource != null)
            lightSource.transform.rotation = Quaternion.Slerp(lightSource.transform.rotation, target, Time.deltaTime * smooth);
    }

    private void Fire()
    {
        if (Input.GetButton("Fire1") && aiming == true && facing >= 0)
        {
            aiming = false;
            anim.SetBool("Shoot", false);
            AudioSource.PlayClipAtPoint(bowSFX, transform.position, hurtVolume);

            if (facing == 0)
                rotation = 0f;
            else if (facing == 1)
                rotation = 180f;
            else if (facing == 2)
                rotation = -90f;
            else if (facing == 3)
                rotation = 90f;

            Instantiate(
                projectile,
                firePoints[facing].position,
                //transform.position,
                Quaternion.identity);

            facing = -1;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        DamageDealer damageDealer = other.gameObject.GetComponent<DamageDealer>();
        if (damageDealer != null && playerHit == false)
        {
            Damage(damageDealer.GetDamage());
        }
    }

    public void Damage(int damageDealt)
    {
        health -= damageDealt;
        if (health > 0)
        {
            playerHit = true;
            AudioSource.PlayClipAtPoint(hurtSFX[Random.Range(0, hurtSFX.Count)], Camera.main.transform.position, hurtVolume);
        }
        else if (health <= 0 && !shadow)
            Death();
        else if (health <= 0 && shadow && alive)
            ShadowDeath();
    }

    private void GracePeriod()
    {
        spriteBlinkTotalTimer += Time.deltaTime;
        if (spriteBlinkTotalTimer >= spriteBlinkTotalDuration)
        {
            spriteBlinkTotalTimer = 0.0f;
            sp.enabled = true;

            playerHit = false;
            return;
        }

        // Flicker sprite
        spriteBlinkTimer += Time.deltaTime;
        if (spriteBlinkTimer >= spriteBlinkMiniDuration)
        {
            spriteBlinkTimer = 0.0f;
            if (sp.enabled)
                sp.enabled = false;
            else
                sp.enabled = true;
        }
    }

    private void Death()
    {
        alive = false;
        if (aud != null)
            aud.Stop();
        AudioSource.PlayClipAtPoint(deathSFX, Camera.main.transform.position, hurtVolume);
        Destroy(gameObject);
        GameObject explosion = Instantiate(deathVFX, transform.position, Quaternion.Euler(0f, 180f, 0f));
        Destroy(explosion, durationOfDeath);
        //FindObjectOfType<Level>().LoadGameOver();
    }

    private void ShadowDeath()
    {
        alive = false;
        AudioSource.PlayClipAtPoint(deathSFX, Camera.main.transform.position, hurtVolume);

        anim.SetTrigger("Shadow Death");
        shadowDeath = true;
        sc.SwitchPlayer();
        //FindObjectOfType<Level>().LoadGameOver();
    }

    public bool GetShadowDeath()
    {
        return shadowDeath;
    }

    public void SetShadowDeath(bool deathStatus)
    {
        shadowDeath = deathStatus;

        if (shadowDeath == true)
        {
            anim.SetTrigger("Shadow Death");
        }
    }

    private void Movement()
    {
        rb.MovePosition(rb.position + movement * updatedMoveSpeed * speedMultiplier * Time.fixedDeltaTime);
    }

    private void Dash()
    {
            switch (dashState)
            {
                case DashState.Ready:
                    var isDashKeyDown = Input.GetKeyDown(KeyCode.Mouse1);
                    if (isDashKeyDown)
                    {
                        savedVelocity = rb.velocity;

                        rb.velocity = new Vector2(movement.x * dashSpeed, movement.y * dashSpeed);
                        dashState = DashState.Dashing;
                    }
                    break;

                case DashState.Dashing:
                    dashing += Time.deltaTime * 3;
                    if (dashing >= dashTimer)
                    {
                        dashing = dashCooldown;
                        rb.velocity = savedVelocity;
                        dashState = DashState.Cooldown;
                    }
                    break;

                case DashState.Cooldown:
                    dashing -= Time.deltaTime;
                    if (dashing <= 0)
                    {
                        dashing = 0;
                        dashState = DashState.Ready;
                    }
                    break;
            }
    }
}
