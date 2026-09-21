# Game Code Review Report
- **Project:** Project2-Survival
- **Engine:** Unity 2022.3.62f2 (LTS)
- **Date:** 2026-09-21
- **Review scope:** Assets/Scripts/ (126 .cs files), Assets/ (full project structure), Scenes (9 .unity), Prefabs (106 .prefab)
- **Reviewed files:** 126 C#, 9 scenes, 106 prefabs
- **Skipped paths:** Library/, Temp/, .git/, Packages/ (read-only), Plugins/ (third-party)
- **Tool limitations:** No Unity Editor access for play-mode profiling; no dotnet CLI for compile check

---

## Summary

Moderate-scale 2D Vampire Survivors-style survival game with well-organized code (24 subdirectories by domain) and clean scene management. However, the project suffers from **critical performance anti-patterns** in hot paths (11 Critical findings), **scene duplication** across 6 near-identical gameplay scenes, and an **oversized MainMenu** (381 GameObjects). The codebase has 17 singletons, ~80 naming violations, and ~40 magic numbers. Performance is the primary concern — repeated `GetComponent` calls in Update/physics callbacks will cause GC pressure and frame drops on mobile.

---

## Score

| Category | Score /100 | Weight |
|---|---|---|
| Project & Scenes Structure | 65 | 20% |
| Code Structure | 58 | 25% |
| Performance | 35 | 30% |
| Clean Code | 62 | 25% |
| **Overall** | **Grade: D (52.1)** | |

---

## Critical (11 findings)

### R-001 — GetComponent<PlayerStats>() in Update (XPGem)
- **Location:** `Assets/Scripts/Player/XPGem.cs:50`
- **Category:** Performance
- **Confidence:** High
- **Why:** `GetComponent<PlayerStats>()` called every frame per XP gem. Gems are pooled and numerous — this is a native engine call per gem per frame causing GC pressure.
- **Fix:** Cache `PlayerStats` reference in `ResolveTarget()` or `OnSpawned()`.

### R-002 — GetComponentInChildren<EnemyHealth> in Update loop (PlayerStats BurnAura)
- **Location:** `Assets/Scripts/Player/PlayerStats.cs:219-220`
- **Category:** Performance
- **Confidence:** High
- **Why:** `GetComponentInChildren`/`GetComponentInParent` called inside `Physics2D.OverlapCircleNonAlloc` loop in Update. Each enemy hit triggers up to 2 component lookups per frame.
- **Fix:** Use a component cache on enemy GameObjects or require `EnemyHealth` on the root collider.

### R-003 — GetComponentInChildren<EnemyHealth> in nested Update loop (CircleWallManager)
- **Location:** `Assets/Scripts/Enemy/CircleWallManager.cs:197-198`
- **Category:** Performance
- **Confidence:** High
- **Why:** Called inside nested loop (walls → enemies per wall) within Update. Two component lookups per enemy per frame for dozens of enemies.
- **Fix:** Cache `EnemyHealth` reference when enemies are added to `WallEntry.enemies`.

### R-004 — GetComponentInParent<EnemyHealth> in Update-triggered methods (ThunderCloudController)
- **Location:** `Assets/Scripts/SummonSkills/ThunderCloudController.cs:89-90, 127-128, 141-142`
- **Category:** Performance
- **Confidence:** High
- **Why:** Called inside `Physics2D.OverlapCircleAll` loops within `Strike()` and `FieldTick()`, both triggered from Update. Up to 6 component lookups per enemy per strike.
- **Fix:** Use `TryGetComponent` on the root or cached lookup on the collider.

### R-005 — Multiple GetComponentInParent in Update-triggered flamethrower (FlamethrowerController)
- **Location:** `Assets/Scripts/Weapon/FlamethrowerController.cs:299, 307-308, 318, 336, 349`
- **Category:** Performance
- **Confidence:** High
- **Why:** Up to 4 `GetComponentInParent` calls per collider hit, every frame from Update. `FireLastAmmoBurst` runs 10-iteration inner loop with the same pattern.
- **Fix:** Cache damage/collision data on enemy GameObjects at spawn time.

### R-006 — 6+ GetComponent calls per bullet collision (Bullet)
- **Location:** `Assets/Scripts/Weapon/Bullet.cs:113, 118-120, 126, 146-148, 176-178, 192-193, 233-234, 246-247`
- **Category:** Performance
- **Confidence:** High
- **Why:** Up to 6+ `GetComponent`/`GetComponentInChildren`/`GetComponentInParent` per bullet collision. Bullets fire rapidly, multiple collide per frame. Among the hottest code paths.
- **Fix:** Use `TryGetComponent` on direct gameObject, avoid parent traversal, implement component cache.

### R-007 — FindGameObjectWithTag("Player") + GetComponent in Update (DebugMenu)
- **Location:** `Assets/Scripts/Debug/DebugMenu.cs:198-199`
- **Category:** Performance
- **Confidence:** High
- **Why:** `FindGameObjectWithTag` scans entire scene hierarchy every frame when `playerStats == null`. One of the most expensive Unity operations in Update.
- **Fix:** Cache in `Start()` or use singleton like `PlayerXP.Instance`.

### R-008 — GetComponent<SpriteFlashEffect> on every TakeDamage (EnemyHealth)
- **Location:** `Assets/Scripts/Enemy/EnemyHealth.cs:86`
- **Category:** Performance
- **Confidence:** High
- **Why:** Called in `TakeDamage()` for every enemy on every bullet hit. Enemies receive damage dozens of times per second.
- **Fix:** Cache in `Awake()`.

### R-009 — GetComponentInChildren<SpriteFlashEffect> on every TakeDamage (PlayerStats)
- **Location:** `Assets/Scripts/Player/PlayerStats.cs:409`
- **Category:** Performance
- **Confidence:** High
- **Why:** Called every time player takes damage with hierarchy search.
- **Fix:** Cache in `Awake()`.

### R-010 — Instantiate in Update-triggered chunk spawning (MapController)
- **Location:** `Assets/Scripts/Map/MapController.cs:160`
- **Category:** Performance
- **Confidence:** High
- **Why:** `Instantiate` called from `ChunkChecker()` which runs every frame in Update. Heavy allocation for prefab instantiation.
- **Fix:** Use object pooling for terrain chunks.

### R-011 — Scene duplication across 6 gameplay scenes
- **Location:** `Assets/Scenes/GameTutorial.unity`, `GameMap1-4.unity`, `GameMapInf.unity`
- **Category:** Project & Scenes Structure
- **Confidence:** High
- **Why:** 6 scenes with ~120 GameObjects each are copy-pasted from a common template. Any bug fix or feature must be replicated 6 times. Major maintenance burden.
- **Fix:** Refactor to single gameplay scene with runtime-loaded map data (ScriptableObjects) or additive scene loading.

---

## Warning (14 findings)

### R-012 — String interpolation in Update (EnemySpawner)
- **Location:** `Assets/Scripts/Enemy/EnemySpawner.cs:267`
- **Category:** Performance
- **Confidence:** High
- **Why:** `$"{minutes:00}:{seconds:00}"` allocates new string every frame.
- **Fix:** Use `StringBuilder` or only update when displayed value changes.

### R-013 — Resources.Load at runtime (GameOverPanel)
- **Location:** `Assets/Scripts/UI/GameOverPanel.cs:57`
- **Category:** Performance
- **Confidence:** High
- **Why:** `Resources.Load<GoldConfig>("GoldConfig")` uses legacy Resources system with poor load times.
- **Fix:** Use `[SerializeField]` ScriptableObject reference.

### R-014 — Resources.Load at runtime (FPSDisplay)
- **Location:** `Assets/Scripts/UI/FPSDisplay.cs:226`
- **Category:** Performance
- **Confidence:** Medium
- **Why:** `Resources.Load<TMP_FontAsset>` as fallback. Only runs once but uses legacy system.
- **Fix:** Use serialized reference to TMP font asset.

### R-015 — FindObjectOfType in Awake/OnDestroy (BossHPUI)
- **Location:** `Assets/Scripts/UI/BossHPUI.cs:23, 29, 41`
- **Category:** Performance
- **Confidence:** High
- **Why:** `FindObjectOfType` scans all loaded objects. Called in Awake and OnDestroy.
- **Fix:** Use static reference/singleton for `EnemySpawner`.

### R-016 — FindObjectOfType on pause event (UIManager)
- **Location:** `Assets/Scripts/Manager/UIManager.cs:226`
- **Category:** Performance
- **Confidence:** Medium
- **Why:** `FindObjectOfType<PausePanel>` on every game state change to Paused.
- **Fix:** Cache in `Start()`.

### R-017 — FindObjectOfType in Start (CharIconUI)
- **Location:** `Assets/Scripts/UI/CharIconUI.cs:14`
- **Category:** Performance
- **Confidence:** Medium
- **Why:** `FindObjectOfType<PlayerEquipment>()` in Start. Slow operation.
- **Fix:** Use singleton or serialized reference.

### R-018 — new WaitForSeconds not cached (BombActive)
- **Location:** `Assets/Scripts/Bomb/BombActive.cs:113, 115`
- **Category:** Performance
- **Confidence:** Medium
- **Why:** New allocation per bomb flash coroutine iteration. Bombs are pooled and frequently spawned.
- **Fix:** Cache common duration values as static readonly fields.

### R-019 — new WaitForSeconds not cached (EnemyHealth)
- **Location:** `Assets/Scripts/Enemy/EnemyHealth.cs:137, 208`
- **Category:** Performance
- **Confidence:** Medium
- **Why:** New allocation per enemy death. Many enemies dying per second creates GC pressure.
- **Fix:** Cache as static readonly: `private static readonly WaitForSeconds s_WaitDeath = new WaitForSeconds(deathFallbackDelay);`

### R-020 — new WaitForSeconds not cached (Bullet)
- **Location:** `Assets/Scripts/Weapon/Bullet.cs:309`
- **Category:** Performance
- **Confidence:** Medium
- **Why:** Called per bullet VFX cleanup. High fire rates create many allocations.
- **Fix:** Cache common delay values.

### R-021 — new WaitForSeconds not cached (AudioManager)
- **Location:** `Assets/Scripts/Audio/AudioManager.cs:496`
- **Category:** Performance
- **Confidence:** Medium
- **Why:** Called per 3D audio source release. Dynamic value but many per second.
- **Fix:** Use dictionary of cached waits for common durations.

### R-022 — MainMenu oversized (381 GameObjects)
- **Location:** `Assets/Scenes/MainMenu.unity`
- **Category:** Project & Scenes Structure
- **Confidence:** High
- **Why:** 381 GameObjects is abnormally large for a menu scene (3x any gameplay scene). Contains hero selection, weapon shop, skin shop, map selection, settings all in one scene.
- **Fix:** Split into minimal MainMenu with additive sub-screen loading.

### R-023 — com.google.ads.mobile unpinned git dependency
- **Location:** `Packages/manifest.json`
- **Category:** Project & Scenes Structure
- **Confidence:** High
- **Why:** AdMob pulled from GitHub HEAD without pinned version/tag. Version can change unpredictably on re-resolve.
- **Fix:** Pin to a specific tag or commit hash.

### R-024 — 5 missing .meta files in FirebaseApp.androidlib
- **Location:** `Assets/Plugins/Android/FirebaseApp.androidlib/`
- **Category:** Project & Scenes Structure
- **Confidence:** High
- **Why:** Missing .meta files cause Unity warnings and version control issues.
- **Fix:** Reimport the plugin or let Unity fix them on next import.

### R-025 — quandev/ folder has non-descriptive name
- **Location:** `Assets/quandev/`
- **Category:** Project & Scenes Structure
- **Confidence:** High
- **Why:** Contains VFX prefabs (Fire, Explosion2, Burn) with developer-specific namespace name.
- **Fix:** Rename to `Assets/VFX/` or merge into `Assets/Prefabs/VFX/`.

---

## Suggestions (12 findings)

### R-026 — Vector3.Distance in loop (MapController)
- **Location:** `Assets/Scripts/Map/MapController.cs:180`
- **Category:** Performance
- **Confidence:** High
- **Why:** `Vector3.Distance` uses sqrt internally. Called in loop over all spawned chunks.
- **Fix:** Use `(playerPos - chunk.transform.position).sqrMagnitude <= maxDistance * maxDistance`.

### R-027 — Vector3.Distance in throttled Update (EnemyVisibilityOptimizer)
- **Location:** `Assets/Scripts/Enemy/EnemyVisibilityOptimizer.cs:50`
- **Category:** Performance
- **Confidence:** High
- **Why:** Called per enemy per check interval (0.3s). Hundreds of enemies = many sqrt calls.
- **Fix:** Use `sqrMagnitude`.

### R-028 — Vector3.Distance in utility (DamagePopup)
- **Location:** `Assets/Scripts/UI/DamagePopup.cs:54`
- **Category:** Performance
- **Confidence:** Medium
- **Why:** Called in `CanMerge()` for popup merging during high-damage scenarios.
- **Fix:** Use `sqrMagnitude`.

### R-029 — Empty Update() method (EquipmentUITween)
- **Location:** `Assets/Scripts/UI/Tweening/EquipmentUITween.cs:14`
- **Category:** Performance
- **Confidence:** High
- **Why:** Empty `Update()` still has overhead — Unity uses reflection to find and invoke it every frame.
- **Fix:** Remove the empty method entirely.

### R-030 — Instantiate in ShowBurnVFX (EnemyHealth)
- **Location:** `Assets/Scripts/Enemy/EnemyHealth.cs:179`
- **Category:** Performance
- **Confidence:** High
- **Why:** `Instantiate` per enemy when burn status first applied. Many enemies trigger simultaneously.
- **Fix:** Use `ObjectPooling` for burn VFX.

### R-031 — Props/Probs folder naming typo
- **Location:** `Assets/Prefabs/Probs/`
- **Category:** Clean Code
- **Confidence:** High
- **Why:** "Probs" is a typo for "Props". Cryptic prefab names (01_33, 03_33, 04_0 3).
- **Fix:** Rename folder to `Props` and improve prefab naming.

### R-032 — EnemyQuan 2.prefab naming anomaly
- **Location:** `Assets/Prefabs/Enemies/EnemyQuan 2.prefab`
- **Category:** Clean Code
- **Confidence:** High
- **Why:** Space with trailing number suggests Editor duplication without proper rename.
- **Fix:** Rename to `EnemyQuan2` or `EnemyQuanB`.

### R-033 — Empty Plugins/Android 1 folder
- **Location:** `Assets/Plugins/Android 1/`
- **Category:** Project & Scenes Structure
- **Confidence:** High
- **Why:** Appears to be accidental duplicate of Android folder. Should be investigated and removed.
- **Fix:** Delete if confirmed empty and unused.

### R-034 — visualscripting package included but likely unused
- **Location:** `Packages/manifest.json`
- **Category:** Project & Scenes Structure
- **Confidence:** Medium
- **Why:** `com.unity.visualscripting` is included but no Bolt/VS references found in code.
- **Fix:** Remove from manifest.json if not used.

### R-035 — Unused built-in modules in manifest
- **Location:** `Packages/manifest.json`
- **Category:** Project & Scenes Structure
- **Confidence:** Medium
- **Why:** Many built-in modules explicitly listed but likely unused (vehicles, VR, XR, cloth).
- **Fix:** Remove unused modules from manifest.

### R-036 — URP2DSceneTemplate orphaned scene
- **Location:** `Assets/Settings/Scenes/URP2DSceneTemplate.unity`
- **Category:** Project & Scenes Structure
- **Confidence:** High
- **Why:** Template artifact not in build. Should be deleted.
- **Fix:** Delete the file.

### R-037 — README.md is gitignored
- **Location:** `.gitignore`
- **Category:** Project & Scenes Structure
- **Confidence:** High
- **Why:** Project README should be tracked in version control.
- **Fix:** Remove `README.md` from .gitignore.

---

## Info (5 findings)

### R-038 — 17 singletons identified
- **Location:** Multiple files
- **Category:** Code Structure
- **Confidence:** High
- **Why:** High singleton count. Most use DontDestroyOnLoad correctly but some lack proper null-guard patterns.
- **Fix:** Audit singleton lifecycle; ensure OnDestroy nulls Instance.

### R-039 — ~80 naming violations (public properties in camelCase)
- **Location:** Multiple files
- **Category:** Clean Code
- **Confidence:** High
- **Why:** Public properties like `CurrentHealth`, `MaxHealth` are PascalCase (correct), but some use inconsistent patterns.
- **Fix:** Standardize private fields to `_camelCase`, public to PascalCase.

### R-040 — ~40 magic numbers in gameplay code
- **Location:** PlayerStats, WeaponController, SynergyManager
- **Category:** Clean Code
- **Confidence:** High
- **Why:** Hardcoded values like `0.25f`, `0.69f`, `5f` without named constants.
- **Fix:** Extract to `const` or `[SerializeField]` named fields.

### R-041 — LevelUpPanel.cs 688 lines (god class)
- **Location:** `Assets/Scripts/UI/LevelUpPanel.cs`
- **Category:** Code Structure
- **Confidence:** High
- **Why:** Contains 310+ lines of duplicated ApplyStat/ApplyStatDirect switch statements.
- **Fix:** Extract to data-driven upgrade system or shared utility class.

### R-042 — PerformanceManager.cs 979 lines
- **Location:** `Assets/Scripts/Optimizer/PerformanceManager.cs`
- **Category:** Code Structure
- **Confidence:** Medium
- **Why:** Large file but well-structured with clear separation of concerns. Acceptable for a core manager.
- **Fix:** No action needed — architectural complexity is justified.

---

## Top priority fixes

1. **R-001 to R-006 (Critical): Cache component references** — The single biggest issue. Implement a component cache system or use `TryGetComponent` on direct GameObjects. This will have the largest impact on mobile performance.

2. **R-011 (Critical): Refactor scene duplication** — 6 copy-pasted gameplay scenes create massive maintenance burden. Refactor to single scene with runtime map data.

3. **R-010 (Critical): Pool terrain chunks** — Replace `Instantiate` in MapController with object pooling.

4. **R-022 (Warning): Split MainMenu** — 381 GameObjects is excessive. Use additive scene loading for sub-menus.

5. **R-029 (Suggestion): Remove empty Update()** — Free performance win in EquipmentUITween.
