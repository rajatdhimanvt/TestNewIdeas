using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 2D block/obstacle placed inside the container for Level Mode.
/// Supports HP, progressive crack sprites, merge proximity damage, Ice physics,
/// Moving Platforms, Key/Lock gates, Magnet Fields, Bumper Pads, Bombs, and Bonus Drops.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DestructibleBlock : MonoBehaviour
{
    private static readonly Collider2D[] magnetOverlapBuffer = new Collider2D[32];

    [Header("Runtime State")]
    [SerializeField] private BlockDefinitionSO definition;
    [SerializeField] private int maxHP = 1;
    [SerializeField] private int currentHP = 1;
    [SerializeField] private bool requiredToClear = true;
    [SerializeField] private bool isDestroyed = false;
    [SerializeField] private bool isLocked = false;
    [SerializeField] private int lockChannel = 1;

    private BoxCollider2D boxCollider;
    private Rigidbody2D kinematicRb;
    private LevelBlockPlacement placementData;
    private LevelDataSO activeLevelData;

    private BlockBehaviorType behaviorType = BlockBehaviorType.Standard;
    private Transform visualRoot;
    private SpriteRenderer mainRenderer;
    private SpriteRenderer crackRenderer;
    private SpriteRenderer lockOverlayRenderer;
    private SpriteRenderer magnetAuraRenderer;
    private TextMeshPro hpText;
    private GameObject hpBadgeObj;
    private Color baseTint = Color.white;
    private Vector2 blockSize = Vector2.one;

    // Moving platform state
    private bool isMoving = false;
    private Vector3 moveStartPosition;
    private Vector3 moveEndPosition;
    private float moveSpeed = 1.4f;
    private float moveTime = 0f;

    // Magnet state
    private float magnetRadius = 2.4f;
    private float magnetForce = 8.5f;

    // Bumper state
    private float bumperImpulse = 7.5f;
    private float lastBumperFlashTime = -10f;

    public BlockDefinitionSO Definition => definition;
    public BlockBehaviorType BehaviorType => behaviorType;
    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public bool IsDestructible => definition == null || definition.isDestructible;
    public bool RequiredToClear => IsDestructible && requiredToClear;
    public bool IsDestroyed => isDestroyed;
    public bool IsLocked => isLocked;
    public int LockChannel => lockChannel;
    public BoxCollider2D Collider => boxCollider;

    public void Initialize(LevelBlockPlacement placement, BlockDefinitionSO def, LevelDataSO levelData)
    {
        placementData = placement;
        definition = def;
        activeLevelData = levelData;
        isDestroyed = false;

        behaviorType = ResolveBehaviorType(def);
        lockChannel = placement != null ? placement.GetEffectiveLockChannel(def) : (def != null ? def.defaultLockChannel : 1);
        isLocked = behaviorType == BlockBehaviorType.LockedGate;

        boxCollider = GetComponent<BoxCollider2D>();

        blockSize = placement != null
            ? new Vector2(Mathf.Max(0.15f, placement.size.x), Mathf.Max(0.1f, placement.size.y))
            : (def != null ? def.defaultSize : new Vector2(0.85f, 0.85f));

        // Root transform stays uniformly scaled (1,1,1) so HP badge and colliders never distort
        transform.localScale = Vector3.one;
        bool isPortal = behaviorType == BlockBehaviorType.PortalEntry || behaviorType == BlockBehaviorType.PortalExit;
        boxCollider.isTrigger = isPortal;
        boxCollider.size = isPortal ? blockSize * 0.72f : blockSize;
        boxCollider.offset = Vector2.zero;

        SetupPhysicsMaterial(def);
        SetupMovingPlatform(placement, def);
        SetupMagnetParameters(placement, def);

        bumperImpulse = def != null ? def.bumperImpulse : 7.5f;

        maxHP = placement != null ? placement.GetEffectiveHP(def) : (def != null ? def.defaultHP : 1);
        currentHP = maxHP;
        requiredToClear = placement != null ? placement.IsRequiredToClear(def) : (def == null || def.requiredToClear);

        baseTint = (placement != null && placement.useCustomColor)
            ? placement.customColor
            : (def != null ? def.baseColor : Color.white);

        BuildVisualHierarchy();
        UpdateDamageVisuals();
    }

    private static BlockBehaviorType ResolveBehaviorType(BlockDefinitionSO def)
    {
        if (def == null) return BlockBehaviorType.Standard;
        if (def.behaviorType != BlockBehaviorType.Standard) return def.behaviorType;
        if (def.explodeOnDestroy) return BlockBehaviorType.ExplosiveBomb;
        if (def.bonusDropsOnDestroy > 0) return BlockBehaviorType.BonusDrops;
        return BlockBehaviorType.Standard;
    }

    private void SetupPhysicsMaterial(BlockDefinitionSO def)
    {
        float bounciness = def != null ? def.bounciness : 0.25f;
        float friction = def != null ? def.friction : 0.4f;

        if (behaviorType == BlockBehaviorType.IceFrozen)
        {
            friction = Mathf.Min(friction, 0.02f);
            bounciness = Mathf.Max(bounciness, 0.3f);
        }
        else if (behaviorType == BlockBehaviorType.BumperPad)
        {
            bounciness = 0.95f;
            friction = 0.05f;
        }

        PhysicsMaterial2D mat = new PhysicsMaterial2D($"{(def != null ? def.blockId : "Block")}_Mat")
        {
            bounciness = bounciness,
            friction = friction
        };
        boxCollider.sharedMaterial = mat;
    }

    private void SetupMovingPlatform(LevelBlockPlacement placement, BlockDefinitionSO def)
    {
        Vector2 offset = placement != null ? placement.GetEffectiveMoveOffset(def) : Vector2.zero;
        isMoving = (behaviorType == BlockBehaviorType.MovingPlatform || (placement != null && placement.overrideMovement))
                   && offset.sqrMagnitude > 0.001f;

        if (isMoving)
        {
            moveStartPosition = transform.position;
            moveEndPosition = moveStartPosition + new Vector3(offset.x, offset.y, 0f);
            moveSpeed = placement != null ? placement.GetEffectiveMoveSpeed(def) : 1.4f;
            moveTime = 0f;

            if (Application.isPlaying)
            {
                kinematicRb = GetComponent<Rigidbody2D>();
                if (kinematicRb == null) kinematicRb = gameObject.AddComponent<Rigidbody2D>();
                kinematicRb.bodyType = RigidbodyType2D.Kinematic;
                kinematicRb.gravityScale = 0f;
                kinematicRb.useFullKinematicContacts = true;
                kinematicRb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
        }
        else
        {
            kinematicRb = GetComponent<Rigidbody2D>();
            if (kinematicRb != null)
            {
                if (Application.isPlaying) Destroy(kinematicRb);
                else DestroyImmediate(kinematicRb);
                kinematicRb = null;
            }
        }
    }

    private void SetupMagnetParameters(LevelBlockPlacement placement, BlockDefinitionSO def)
    {
        magnetRadius = (placement != null && placement.magnetRadiusOverride > 0.1f)
            ? placement.magnetRadiusOverride
            : (def != null ? def.magnetRadius : 2.4f);

        magnetForce = (placement != null && Mathf.Abs(placement.magnetForceOverride) > 0.01f)
            ? placement.magnetForceOverride
            : (def != null ? def.magnetForce : 8.5f);
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying || isDestroyed) return;

        if (isMoving && kinematicRb != null)
        {
            moveTime += Time.fixedDeltaTime * moveSpeed * 1.4f;
            float t = 0.5f * (1f - Mathf.Cos(moveTime));
            Vector2 nextPos = Vector2.Lerp(moveStartPosition, moveEndPosition, t);
            kinematicRb.MovePosition(nextPos);
        }

        if (behaviorType == BlockBehaviorType.MagnetField)
        {
            ApplyMagnetFieldForces();
        }
    }

    private void Update()
    {
        if (!Application.isPlaying || isDestroyed) return;

        if (magnetAuraRenderer != null)
        {
            float pulse = 0.88f + 0.12f * Mathf.Sin(Time.time * 4.5f);
            FitRendererToSize(magnetAuraRenderer, Vector2.one * (magnetRadius * 2f * pulse));
            Color c = magnetAuraRenderer.color;
            c.a = 0.16f + 0.1f * (0.5f + 0.5f * Mathf.Sin(Time.time * 4.5f));
            magnetAuraRenderer.color = c;
        }

        if ((behaviorType == BlockBehaviorType.PortalEntry || behaviorType == BlockBehaviorType.PortalExit) && visualRoot != null)
        {
            float spinSpeed = behaviorType == BlockBehaviorType.PortalEntry ? -150f : 150f;
            visualRoot.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }
    }

    private void ApplyMagnetFieldForces()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        int count = Physics2D.OverlapCircle(transform.position, magnetRadius, filter, magnetOverlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = magnetOverlapBuffer[i];
            if (col == null) continue;

            Ball ball = col.GetComponent<Ball>();
            if (ball == null || !ball.IsDropped || ball.IsMerging || ball.Rigidbody == null) continue;

            Vector2 toMagnet = (Vector2)transform.position - (Vector2)ball.transform.position;
            float dist = toMagnet.magnitude;
            if (dist < 0.08f) continue;

            float falloff = 1f - Mathf.Clamp01(dist / magnetRadius) * 0.5f;
            ball.Rigidbody.AddForce(toMagnet.normalized * (magnetForce * falloff), ForceMode2D.Force);
        }
    }

    private void BuildVisualHierarchy()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        GameObject visualObj = new GameObject("Visual");
        visualObj.transform.SetParent(transform, false);
        visualRoot = visualObj.transform;

        mainRenderer = visualObj.AddComponent<SpriteRenderer>();
        Sprite sprite = definition != null && definition.baseSprite != null
            ? definition.baseSprite
            : (IsDestructible ? BlockRuntimeVisuals.GetDefaultCrateSprite() : BlockRuntimeVisuals.GetDefaultShelfSprite());

        mainRenderer.sprite = sprite;
        mainRenderer.color = isLocked ? Color.Lerp(baseTint, new Color(0.28f, 0.30f, 0.38f, 1f), 0.45f) : baseTint;
        mainRenderer.sortingOrder = 5;
        FitRendererToSize(mainRenderer, blockSize);

        // Crack overlay child
        if (IsDestructible)
        {
            GameObject crackObj = new GameObject("CrackOverlay");
            crackObj.transform.SetParent(visualRoot, false);
            crackObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);

            crackRenderer = crackObj.AddComponent<SpriteRenderer>();
            crackRenderer.sortingOrder = 6;
            crackRenderer.color = behaviorType == BlockBehaviorType.IceFrozen
                ? new Color(0.84f, 0.96f, 1f, 0.92f)
                : Color.white;
            crackRenderer.enabled = false;
        }

        // Padlock overlay for LockedGate blocks
        if (behaviorType == BlockBehaviorType.LockedGate && isLocked)
        {
            GameObject lockObj = new GameObject("LockOverlay");
            lockObj.transform.SetParent(transform, false);
            lockObj.transform.localPosition = new Vector3(0f, 0f, -0.06f);
            lockObj.transform.rotation = Quaternion.identity;

            lockOverlayRenderer = lockObj.AddComponent<SpriteRenderer>();
            lockOverlayRenderer.sprite = definition != null && definition.lockOverlaySprite != null
                ? definition.lockOverlaySprite
                : BlockRuntimeVisuals.GetDefaultPadlockSprite();
            lockOverlayRenderer.sortingOrder = 9;
            float lockSize = Mathf.Clamp(Mathf.Min(blockSize.x, blockSize.y) * 0.62f, 0.36f, 0.62f);
            FitRendererToSize(lockOverlayRenderer, new Vector2(lockSize, lockSize));
        }

        // Magnetic aura ring visual
        if (behaviorType == BlockBehaviorType.MagnetField)
        {
            GameObject auraObj = new GameObject("MagnetAura");
            auraObj.transform.SetParent(transform, false);
            auraObj.transform.localPosition = new Vector3(0f, 0f, 0.05f);

            magnetAuraRenderer = auraObj.AddComponent<SpriteRenderer>();
            magnetAuraRenderer.sprite = BlockRuntimeVisuals.GetRingSprite();
            magnetAuraRenderer.color = magnetForce >= 0f
                ? new Color(0.28f, 0.78f, 1f, 0.22f)
                : new Color(1f, 0.35f, 0.55f, 0.22f);
            magnetAuraRenderer.sortingOrder = 1;
            FitRendererToSize(magnetAuraRenderer, Vector2.one * (magnetRadius * 2f));
        }

        // Optional attached support shelf directly underneath
        if (placementData != null && placementData.addSupportShelf)
        {
            BuildAttachedShelf();
        }

        // Crisp world-space HP badge (unrotated & unstretched)
        if (IsDestructible && (definition == null || definition.showHPBadge))
        {
            BuildHPBadge();
        }
    }

    private void BuildAttachedShelf()
    {
        float shelfHeight = 0.14f;
        float shelfWidth = blockSize.x + 0.35f;

        GameObject shelfObj = new GameObject("AttachedSupportShelf");
        shelfObj.transform.SetParent(transform, false);
        shelfObj.transform.localPosition = new Vector3(0f, -(blockSize.y * 0.5f) - (shelfHeight * 0.5f) - 0.01f, 0.02f);

        BoxCollider2D shelfCol = shelfObj.AddComponent<BoxCollider2D>();
        shelfCol.size = new Vector2(shelfWidth, shelfHeight);

        GameObject shelfVis = new GameObject("ShelfSprite");
        shelfVis.transform.SetParent(shelfObj.transform, false);
        SpriteRenderer visSr = shelfVis.AddComponent<SpriteRenderer>();
        visSr.sprite = BlockRuntimeVisuals.GetDefaultShelfSprite();
        visSr.sortingOrder = 4;
        FitRendererToSize(visSr, new Vector2(shelfWidth, shelfHeight));
    }

    private void BuildHPBadge()
    {
        hpBadgeObj = new GameObject("HPBadge");
        hpBadgeObj.transform.SetParent(transform, false);
        hpBadgeObj.transform.localPosition = new Vector3(0f, 0f, -0.05f);
        hpBadgeObj.transform.rotation = Quaternion.identity;

        SpriteRenderer bg = hpBadgeObj.AddComponent<SpriteRenderer>();
        bg.sprite = BlockRuntimeVisuals.GetChipSprite();
        bg.color = new Color(0.08f, 0.09f, 0.13f, 0.82f);
        bg.sortingOrder = 7;

        float badgeSize = Mathf.Clamp(Mathf.Min(blockSize.x, blockSize.y) * 0.42f, 0.26f, 0.44f);
        FitRendererToSize(bg, new Vector2(badgeSize, badgeSize));

        GameObject textObj = new GameObject("HPText");
        textObj.transform.SetParent(hpBadgeObj.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);

        hpText = textObj.AddComponent<TextMeshPro>();
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.fontStyle = FontStyles.Bold;
        hpText.fontSize = Mathf.Clamp(badgeSize * 7.8f, 2.2f, 3.6f);
        hpText.color = Color.white;
        hpText.sortingOrder = 8;

        RectTransform rt = hpText.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1.5f, 1.5f);

        if (isLocked)
        {
            hpBadgeObj.SetActive(false);
        }
    }

    private static void FitRendererToSize(SpriteRenderer sr, Vector2 targetWorldSize)
    {
        if (sr == null || sr.sprite == null) return;
        Vector2 spriteSize = sr.sprite.bounds.size;
        float sx = spriteSize.x > 0.0001f ? targetWorldSize.x / spriteSize.x : targetWorldSize.x;
        float sy = spriteSize.y > 0.0001f ? targetWorldSize.y / spriteSize.y : targetWorldSize.y;
        sr.transform.localScale = new Vector3(sx, sy, 1f);
    }

    public bool CanTakeDamageFromMerge(Vector2 mergeCenter, float baseBlastRadius)
    {
        if (isDestroyed || !IsDestructible) return false;
        if (definition != null && !definition.damageOnNearbyMerge) return false;
        if (boxCollider == null) return false;

        float extra = definition != null ? definition.extraMergeHitRadius : 0f;
        float effectiveRadius = Mathf.Max(0.1f, baseBlastRadius + extra);

        Vector2 closest = boxCollider.ClosestPoint(mergeCenter);
        float distSqr = (closest - mergeCenter).sqrMagnitude;
        return distSqr <= (effectiveRadius * effectiveRadius);
    }

    public void TakeDamage(int amount, Vector2 hitSourcePosition, bool isExplosion = false)
    {
        if (isDestroyed || !IsDestructible || amount <= 0) return;

        if (isLocked)
        {
            BlockRuntimeVisuals.SpawnFloatingTag("LOCKED!", transform.position + Vector3.up * 0.32f, new Color(1f, 0.78f, 0.25f), 3.6f);
            GameEvents.OnPlaySFX?.Invoke("LockedClink");
            if (lockOverlayRenderer != null)
            {
                lockOverlayRenderer.transform.DOKill();
                lockOverlayRenderer.transform.DOShakePosition(0.2f, 0.06f, 20, 90f, false, true).SetLink(gameObject);
            }
            return;
        }

        currentHP = Mathf.Max(0, currentHP - amount);

        Color hitColor = behaviorType == BlockBehaviorType.IceFrozen
            ? new Color(0.6f, 0.92f, 1f)
            : (definition != null ? definition.hitParticleColor : new Color(1f, 0.78f, 0.35f));

        BlockRuntimeVisuals.SpawnDebrisBurst(transform.position, hitColor, count: 8, speed: 2.8f, scale: 0.11f);
        BlockRuntimeVisuals.SpawnFloatingTag($"-{amount}", transform.position + Vector3.up * 0.25f, new Color(1f, 0.42f, 0.35f), 3.6f);

        if (currentHP <= 0)
        {
            DestroyBlock(isExplosion);
        }
        else
        {
            UpdateDamageVisuals();
            PlayHitAnimation();

            string sfx = behaviorType == BlockBehaviorType.IceFrozen
                ? "IceCrack"
                : (definition != null && !string.IsNullOrEmpty(definition.hitSoundKey) ? definition.hitSoundKey : "BlockHit");
            GameEvents.OnPlaySFX?.Invoke(sfx);
        }
    }

    public void Unlock()
    {
        if (isDestroyed || !isLocked) return;
        isLocked = false;

        if (mainRenderer != null)
        {
            mainRenderer.DOKill();
            mainRenderer.DOColor(baseTint, 0.28f).SetLink(gameObject);
        }

        if (hpBadgeObj != null)
        {
            hpBadgeObj.SetActive(true);
            hpBadgeObj.transform.localScale = Vector3.zero;
            hpBadgeObj.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetLink(gameObject);
        }

        if (lockOverlayRenderer != null)
        {
            SpriteRenderer lockSr = lockOverlayRenderer;
            lockOverlayRenderer = null;
            lockSr.transform.DOKill();
            lockSr.transform.DOMoveY(lockSr.transform.position.y + 0.65f, 0.3f).SetEase(Ease.OutCubic).SetLink(lockSr.gameObject);
            lockSr.DOFade(0f, 0.3f).SetLink(lockSr.gameObject).OnComplete(() =>
            {
                if (lockSr != null) Destroy(lockSr.gameObject);
            });
        }

        BlockRuntimeVisuals.SpawnFloatingTag("UNLOCKED!", transform.position + Vector3.up * 0.35f, new Color(1f, 0.88f, 0.28f), 4.0f);
        PlayHitAnimation();
        UpdateDamageVisuals();
    }

    private void UpdateDamageVisuals()
    {
        if (hpBadgeObj != null && hpText != null)
        {
            hpText.text = currentHP.ToString();
            hpText.color = (maxHP > 1 && currentHP == 1)
                ? new Color(1f, 0.45f, 0.35f)
                : Color.white;
        }

        if (crackRenderer != null)
        {
            if (currentHP < maxHP && maxHP > 0)
            {
                Sprite crackSprite = definition != null ? definition.GetCrackSpriteForHealth(currentHP, maxHP) : null;
                if (crackSprite == null)
                {
                    float dmgRatio = 1f - Mathf.Clamp01((float)currentHP / maxHP);
                    int stage = Mathf.Clamp(Mathf.FloorToInt(dmgRatio * 3f), 0, 2);
                    crackSprite = BlockRuntimeVisuals.GetFallbackCrackSprite(stage, 3);
                }

                if (crackSprite != null)
                {
                    crackRenderer.sprite = crackSprite;
                    crackRenderer.enabled = true;
                    if (mainRenderer != null && mainRenderer.sprite != null)
                    {
                        Vector2 mainBounds = mainRenderer.sprite.bounds.size;
                        Vector2 crackBounds = crackSprite.bounds.size;
                        float sx = crackBounds.x > 0.0001f ? mainBounds.x / crackBounds.x : 1f;
                        float sy = crackBounds.y > 0.0001f ? mainBounds.y / crackBounds.y : 1f;
                        crackRenderer.transform.localScale = new Vector3(sx, sy, 1f);
                    }
                }
            }
            else
            {
                crackRenderer.enabled = false;
            }
        }
    }

    private void PlayHitAnimation()
    {
        if (visualRoot != null)
        {
            visualRoot.DOKill();
            visualRoot.localPosition = Vector3.zero;
            FitRendererToSize(mainRenderer, blockSize);
            Vector3 baseScale = visualRoot.localScale;
            visualRoot.DOPunchScale(baseScale * 0.16f, 0.2f, 8, 0.6f).SetLink(gameObject);
            visualRoot.DOShakePosition(0.16f, 0.06f, 18, 90f, false, true).SetLink(gameObject);
        }

        if (mainRenderer != null)
        {
            mainRenderer.DOKill();
            mainRenderer.color = Color.Lerp(baseTint, Color.white, 0.65f);
            mainRenderer.DOColor(isLocked ? Color.Lerp(baseTint, new Color(0.28f, 0.30f, 0.38f, 1f), 0.45f) : baseTint, 0.22f).SetLink(gameObject);
        }
    }

    private void DestroyBlock(bool fromExplosion)
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (boxCollider != null) boxCollider.enabled = false;

        Vector3 pos = transform.position;
        Color destroyCol = behaviorType == BlockBehaviorType.IceFrozen
            ? new Color(0.55f, 0.92f, 1f)
            : (definition != null ? definition.destroyParticleColor : new Color(1f, 0.52f, 0.18f));
        BlockRuntimeVisuals.SpawnDebrisBurst(pos, destroyCol, count: 20, speed: 4.5f, scale: 0.15f);

        int rewardScore = definition != null ? definition.scoreReward : 50;
        if (rewardScore > 0)
        {
            GameEvents.OnScoreChanged?.Invoke(rewardScore);
            GameEvents.OnScoreAddedAtPosition?.Invoke(rewardScore, pos);
        }

        int bonusDrops = definition != null ? definition.bonusDropsOnDestroy : 0;
        if (bonusDrops > 0)
        {
            GameEvents.OnBonusDropsEarned?.Invoke(bonusDrops);
            BlockRuntimeVisuals.SpawnFloatingTag($"+{bonusDrops} DROPS!", pos + Vector3.up * 0.45f, new Color(0.3f, 1f, 0.55f), 4.4f);
            GameEvents.OnPlaySFX?.Invoke("BonusDrop");
        }

        string destroySfx = behaviorType == BlockBehaviorType.IceFrozen
            ? "IceCrack"
            : (definition != null && !string.IsNullOrEmpty(definition.destroySoundKey) ? definition.destroySoundKey : "BlockBreak");
        GameEvents.OnPlaySFX?.Invoke(destroySfx);

        bool isBomb = behaviorType == BlockBehaviorType.ExplosiveBomb || (definition != null && definition.explodeOnDestroy);
        CameraShake2D.TriggerShake(isBomb ? 0.22f : 0.08f, isBomb ? 0.28f : 0.14f);

        if (LevelBlockManager.HasInstance)
        {
            LevelBlockManager.Instance.NotifyBlockDestroyed(this);
        }
        GameEvents.OnBlockDestroyed?.Invoke(this);

        // Key Block unlocks matching Locked Gates
        if (behaviorType == BlockBehaviorType.KeyTrigger && LevelBlockManager.HasInstance)
        {
            BlockRuntimeVisuals.SpawnFloatingTag("KEY!", pos + Vector3.up * 0.45f, new Color(1f, 0.9f, 0.25f), 4.5f);
            LevelBlockManager.Instance.TriggerKeyUnlock(pos, lockChannel);
        }

        // Bomb / TNT chain explosion
        if (isBomb && LevelBlockManager.HasInstance)
        {
            float expRadius = definition != null ? definition.explosionRadius : 1.9f;
            int expDamage = definition != null ? definition.explosionDamage : 2;
            BlockRuntimeVisuals.SpawnFloatingTag("BOOM!", pos, new Color(1f, 0.35f, 0.15f), 5.0f);
            GameEvents.OnPlaySFX?.Invoke("Explosion");
            LevelBlockManager.Instance.TriggerBlockExplosion(pos, expRadius, expDamage, this);
        }

        transform.DOKill();
        transform.DOScale(Vector3.zero, 0.16f).SetEase(Ease.InBack).SetLink(gameObject)
            .OnComplete(() =>
            {
                if (gameObject != null) Destroy(gameObject);
            });
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyed) return;

        Ball ball = collision.gameObject.GetComponent<Ball>();
        if (ball == null || !ball.IsDropped || ball.IsMerging) return;

        if (behaviorType == BlockBehaviorType.BumperPad && ball.Rigidbody != null)
        {
            Vector2 pushDir = ((Vector2)ball.transform.position - (Vector2)transform.position).normalized;
            if (pushDir.sqrMagnitude < 0.01f) pushDir = Vector2.up;
            ball.Rigidbody.linearVelocity = pushDir * bumperImpulse;

            if (Time.time - lastBumperFlashTime > 0.08f)
            {
                lastBumperFlashTime = Time.time;
                PlayHitAnimation();
                BlockRuntimeVisuals.SpawnShockwaveRing(transform.position, Mathf.Max(blockSize.x, blockSize.y) * 0.95f, new Color(1f, 0.35f, 0.85f, 0.8f), 0.22f);
                GameEvents.OnPlaySFX?.Invoke("Bumper");
            }
            return;
        }

        if (IsDestructible && definition != null && definition.damageOnBallCollision)
        {
            TakeDamage(1, collision.GetContact(0).point);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed || behaviorType != BlockBehaviorType.PortalEntry) return;

        Ball ball = other.GetComponent<Ball>();
        if (ball != null && ball.CanUsePortal() && LevelBlockManager.HasInstance)
        {
            LevelBlockManager.Instance.TryTeleportBall(ball, this);
        }
    }

    public void PlayPortalPulse(Color ringColor)
    {
        if (visualRoot != null)
        {
            visualRoot.DOKill();
            FitRendererToSize(mainRenderer, blockSize);
            Vector3 baseScale = visualRoot.localScale;
            visualRoot.DOPunchScale(baseScale * 0.25f, 0.22f, 6, 0.6f).SetLink(gameObject);
        }
        BlockRuntimeVisuals.SpawnShockwaveRing(transform.position, Mathf.Max(blockSize.x, blockSize.y) * 0.9f, ringColor, 0.24f);
    }
}
