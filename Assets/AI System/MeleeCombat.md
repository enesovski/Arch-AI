# Melee approach, rush, attack, withdrawal and circling

The loop is enabled on `P_Entity_Tripix`, `P_Entity_Iribax` and `P_Entity_Nimblix` through each MovementModule's own **Melee Combat Profile**. Other entities can opt in by assigning a profile there. Create a separate profile to tune a species or choose Front, Flank or Unrestricted positioning.

The existing melee Behavior graph stays responsible for combat and chase/attack transitions. Its ChaseTarget action delegates tactical movement to MovementModule while a profile is assigned. MovementModule lazily creates one plain `MeleeCombatMovement` controller for an opted-in entity, which holds the phase, destinations, positioning decisions and saved locomotion settings. It adds no component or independent Update loop. AttackModule supplies cooldown readiness and attack clearance and checks positioning eligibility before committing; it owns range queries, attack animation commitment and animation-event damage.

## Where to tune

| Inspector | Settings | Effect |
| --- | --- | --- |
| MovementModule -> Movement Profile -> Run | Run Movement Speed | Speed requested during Approach and Rush. |
| Movement Profile -> Combat Movement | Withdrawal Speed, Circling Speed | Physical movement speed during retreat and circling; zero uses Walk Movement Speed. |
| Movement Profile -> Combat Movement -> Circling Steps | Step And Pause, Step Distance, Pause Between Steps | Optional fixed short circle goals followed by random pauses on arrival. Step Distance caps sideways movement; spacing/sector corrections can require longer travel. |
| Movement Profile -> Combat Movement | Attack Approach Spacing | Fraction of usable attack reach at the Rush endpoint. Tripix uses 0.9 (90%); higher values leave more clearance without increasing damage reach. |
| Movement Profile -> Combat Movement | Face Target While Repositioning | Enables facing the target while strafing/backpedaling. |
| MovementModule -> Melee Combat Profile | Positioning, Holding Distance | Preferred front/flank/unrestricted sector and retreat/circle clearance. |
| Melee Combat Profile -> Recovery | Circle Duration, Circle Duration Variance | Time before another approach, subject to attack cooldowns. |
| Advanced foldouts in the profiles | Navigation/facing tuning, withdrawal limits, blocked-sector fallback | Less commonly adjusted controls, retained to preserve existing tuning. |

Front/flank angle controls appear only for their positioning style. The orbit waypoint angular speed is retained under Movement Profile -> Combat Movement -> Advanced. Circling goals lead the robot's current position by a short arc; Circling Speed and Orbit Goal Speed jointly limit that lead. Direction reverses when the robot approaches its sector edge, with the flank side retained independently of orbit direction.

The migrated Tripix prefab assigns the same combat profile GUID to MovementModule. MovementProfile now contains all nine moved settings with their original values. AttackModule's old profile field was removed. The profile script and melee range script were relocated/renamed with their original meta GUIDs. Newly added controller files must be included in source control along with the prefab and both profile assets.

## Tripix starting settings

- Front arc: 70 degrees on each side of the target's horizontal facing.
- Sector recovery and circling aim inside the front/flank sector, with a margin derived from stopping tolerance, sampling radius and holding radius. Sampled final goals must also leave room for arrival tolerance. Reached waypoints outside the Rush sector are replaced. Recovery tracks actual angular improvement; complete paths without progress use the existing 2-second fallback grace instead of waiting indefinitely.
- Attack Approach Spacing: 0.9, increased from 0.75. Rush uses at most 0.05 metres of stopping tolerance to avoid stopping outside attack range as the endpoint moves farther away. Attack ranges and damage capsule dimensions are unchanged.
- Once a melee cooldown is ready and the robot is in the preferred sector, Rush closes directly toward the target's attack clearance at the movement profile's run speed (Tripix: 9 metres/second). Automatic braking is disabled during Rush and circling. The chase action stops for a valid attack; if the planned Rush endpoint is reached first, the controller replaces its stopped path and corrects clearance using actual surface range for ready attacks. Each correction is limited to 0.5 metres. Turning out of the preferred sector returns the robot to Approach. Approach/withdrawal retain automatic braking, and the previous setting is restored when chase ends.
- Holding distance: 6 metres from the target's navigation footprint to the robot root. Large attack origins can expand this distance to keep the holding position outside melee reach.
- Movement Profile -> Combat Movement: Withdrawal Speed 4.5 metres/second; Circling Speed 3.5 metres/second. Set either to zero to use the movement profile's walk speed.
- Withdrawal: at least 1 second, followed by 2.5–3 seconds of circling before another approach. A 3.5 second withdrawal timeout handles blocked retreats. Orbit Goal Speed is 35 degrees/second; both this angular goal speed and Circling Speed influence how quickly the robot circles.
- Planning: once per 0.35 seconds, with retained destinations and at most three complete-path candidates per decision. Navigation sampling uses the agent's type and allowed areas; path calculation uses its area costs.
- When the preferred position stays unreachable for 2 seconds, the robot can approach/attack from a reachable fallback. Range, facing and recovery gates still apply.

Rush rejects sampled points within its stopping tolerance of the robot when a ready attack is still outside surface range. A complete path to the same position cannot correct that range, so the other bounded approach candidates are tried. This covers NavMesh edges and locally clipped goals; valid range may still wait briefly for facing. Movement gizmos include the melee phase alongside navigation status, since Arrived alone does not mean an attack is ready.

Combat sampling first uses the proposed point's height, then the agent's current NavMesh height. If both fail, one downward ray using the Ground Fitter's ground layers resolves the actual floor at that horizontal point, followed by a final NavMesh sample. This handles intermediate slope heights that match neither endpoint without widening horizontal sampling. The ray spans the two known heights plus padding based on agent height. There are at most three samples and one fallback ray per candidate, still at most three complete-path candidates per planning tick. Reset paths clear their debug validity; No Path is shown when no destination is active.

Facing follows the target smoothly. A player turning around can briefly leave a robot behind them; the robot works back around the target in short arc steps. The facing source defaults to GameEntity's root. The current player camera controller rotates that root and ClientNetworkTransform replicates it to the server. GameEntity also has an optional Combat Facing override for other entity layouts.

During tactical locomotion the NavMesh agent owns translation, including withdrawal while facing the target. AIGroundFitter consumes animation root motion in that phase. The original root-motion setting, rotation mode, stopping distance and braking are restored when the chase action ends. Locomotion uses the existing animation controller and procedural legs.

For profiled melee attacks with Stop During Attack enabled, AIGroundFitter also suppresses root translation throughout attack commitment, even after ChaseTarget ends. This prevents the attack animation pushing a stopped entity into its target. Attacks that allow movement and entities without a melee combat profile keep their existing root-motion behavior. The updated AIGroundFitter.cs outside Systems must be included in source control with this change.

## Iribax and Nimblix starting settings

Both use the existing melee Behavior graph and the same controller as Tripix, with independent species profiles. Boundary recovery, surface-based attack reach and stopped-attack root-motion suppression apply automatically.

Normal AI attack clip exits in their animator controllers wait for IsAttacking to become false. This keeps the visual attack within one commitment and prevents early exit/re-entry before withdrawal begins.

| Setting | Iribax | Nimblix |
| --- | --- | --- |
| Rush speed | 10 m/s | 7 m/s |
| Withdrawal / circling speeds | 7 / 7 m/s | 3.5 / 3 m/s |
| Attack Approach Spacing | 0.9 | 0.9 |
| Holding distance | 6 m | 8 m |
| Minimum withdrawal / timeout | 0.3 / 2 s | 1.2 / 4 s |
| Circling duration | 0.5–0.65 s | 3–3.5 s |
| Orbit goal speed | 55 degrees/s | 25 degrees/s |
| Planning interval | 0.15 s | 0.35 s |

Iribax plans more frequently to follow its faster locomotion. Each decision still checks at most three complete-path candidates; Tripix and Nimblix retain their existing planning cadence.

Holding distance is clearance from the target's navigation footprint to the entity root, expanded if needed for attack reach. Both prefer the target's front 70-degree sector and retain the 2-second fallback grace. Iribax uses a quick hit-and-run tuning pass: Claw cooldown 2 seconds, Bite and Tail 4 seconds. Cooldowns begin after each attack ends and can extend circling if none is ready. Mandatory withdrawal/circling prevents Nimblix's zero-cooldown Slash from immediately attacking again. Check all three Iribax attacks and both Nimblix attacks in game, including several robots near walls and a remote player.

## Nimblix step and pause tuning

Nimblix enables **Movement Profile -> Combat Movement -> Circling Steps -> Step And Pause**. Starting values: Step Distance 1.5 m, Pause Between Steps 0.8–1.5 s, Circling Speed 2.5 m/s, Orbit Goal Speed 12 degrees/s, Withdrawal Speed 3.5 m/s. Holding Distance remains 10 m. Continuous circling remains the default for profiles without Step And Pause enabled.

A short step keeps its destination until arrival, with braking enabled. On arrival it stops translation and starts one random pause. Pauses count toward Circle Duration; a ready attack after that recovery duration cancels the pause and resumes approach. Significant holding-distance changes or leaving the preferred live sector also cancel the pause/step so the robot can reposition. Look-at rotation remains active when Face Target While Repositioning is enabled. Fixed steps that make no progress for Blocked Sector Grace are released for replanning. No path queries run while an unchanged step or pause is retained. MovementModule exposes **Circling Paused**, and gizmos show **Circle Pause** during an intentional wait. No roar animation is triggered yet.

## Range and body geometry

Melee MinRange/MaxRange now measure from the configured attack point to the closest active, non-trigger target collider on the melee module's attackable layers. Maximum readiness reach is bounded by the attack capsule's length plus radius. Ranged attacks retain center-to-center range semantics. Review authored melee ranges when tuning other species, especially attacks with large offsets or lunges.

GameEntity lazily caches owned collider references; it excludes nested entities. Its optional Combat Colliders array can restrict complex robots to a few body/hurtbox shapes. Call `RebuildCombatColliders()` after adding or replacing those shapes at runtime. Disabled/inactive colliders are checked on each geometry query. Melee range samples are reused within each frame. Conservative bounds are used for movement clearance; actual collider shapes determine attack range. Animation-event capsule overlap remains the final damage check.

## In-game checks

1. On a host/server, aggro one Tripix while standing still, first in open space at 10–15 metres, then closer: approach, rush, attack, withdraw and front-arc circling should repeat. MovementModule's read-only Melee Phase shows Rush during the final close. Check that it runs in decisively and stops for the attack without passing through the target.
2. Walk, run and turn 180 degrees. Check recovery of the front sector without rapid facing oscillation.
   Also hold a Tripix just outside the front sector (about 72 degrees): it should move inward and rush, or choose a reachable fallback after the grace if blocked, instead of waiting at the boundary. Check on both sides of the player.
3. Fight near a wall and in a narrow passage. Check reachable alternatives and the fallback when there is no room to retreat.
4. Use a large robot target. Check attacks near its body surface without requiring access to its root position.
5. Repeat with several Tripixes and a remote player. This change provides individual positioning; shared attack permits and swarm coordination remain future work.

Editor regression tests are in `Tests/Editor/MeleeCombatTests.cs`.

Tripix's attack definition references the imported 2.5-second attack clip. The animator regression check requires Jump Attack exits to wait for IsAttacking to become false, preventing re-entry through Alarmed under the same attack command. The current controller is missing that condition; its independently edited exit timing was preserved pending clarification. Multiple animation-event hits remain a single combo. AttackModule rejects animation events outside commitment or from a different attack definition.
