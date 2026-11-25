using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace DoctorHelp
{
    // End-of-day cutscene played live in Endday_Cutscene (replaces the Success/Fail videos).
    // All patients treated correctly: 1-2 of them stand by the bed, the player walks in, asks, one thanks him.
    // Someone died (any wrong treatment): one dead patient drinks, dies, rises as a Ghost; other dead patients
    // come in as Ghosts. Correct police reports (jailed) never appear. The camera is fixed; the scene's
    // gameplay objects listed in hideDuringCutscene are switched off before they wake up.
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class EnddayCutscene : MonoBehaviour
    {
        private static readonly int IdleState = Animator.StringToHash("Base Layer.NPC_Idle");
        private static readonly int DrinkState = Animator.StringToHash("Base Layer.NPC_Drink");
        private static readonly int DieState = Animator.StringToHash("Base Layer.NPC_Die");
        private const string WorldSuffix = "_world";

        [Header("Scene")]
        [SerializeField] private Camera sceneCamera;
        [Tooltip("Full-screen black cover; saved opaque so the scene never flashes on load.")]
        [SerializeField] private CanvasGroup blackCover;
        [SerializeField] private CanvasGroup laterThatNightText;
        [Tooltip("Turned off before they wake up: Player Home systems that must not run in the cutscene.")]
        [SerializeField] private GameObject[] hideDuringCutscene = Array.Empty<GameObject>();

        [Header("Places (drag to adjust)")]
        [SerializeField] private Transform playerStart;
        [Tooltip("Where the player stops (by the metal cabinet).")]
        [SerializeField] private Transform playerStop;
        [Tooltip("Where patients stand by the bed: first spot for one patient, both for two.")]
        [SerializeField] private Transform[] patientSpots = Array.Empty<Transform>();
        [Tooltip("Ghosts enter and leave on this side, off camera.")]
        [SerializeField] private Transform ghostDoor;

        [Header("Characters")]
        [SerializeField] private PlayerHomeCharacterAnimation player;
        [SerializeField] private NpcDialogue playerDialogue;
        [SerializeField] private NpcProfile npcProfile;
        [Tooltip("Empty: Resources/NpcWorldCatalog.")]
        [SerializeField] private NpcCatalog worldCatalog;
        [SerializeField] private GhostDialogue ghostPrefab;
        [SerializeField] private Shader examinationShader;
        [SerializeField] private Material walkingSpriteMaterial;
        [Tooltip("Patients turn to face the door on the right, where the player comes in.")]
        [SerializeField] private bool patientsFaceRight = true;
        [SerializeField] private int characterSortingOrder = 5;
        [Tooltip("Speech bubbles draw above every character sprite and the foreground.")]
        [SerializeField] private int bubbleSortingOrder = 40;
        [Tooltip("Raise player and patient speech bubbles above their heads, in world units.")]
        [SerializeField, Min(0f)] private float dialogueLift = 3f;

        [Header("Death")]
        [SerializeField] private GameObject eyeHurt;
        [SerializeField] private GameObject mouthHurt;
        [SerializeField] private GameObject eyeDie;
        [SerializeField] private GameObject mouthDie;
        [SerializeField, Min(0f)] private float hurtFaceSeconds = .5f;
        [SerializeField] private Color dyingSkinColor = new Color(.64f, .66f, .84f, 1f);
        [SerializeField, Min(0f)] private float dyingSkinSeconds = 1f;
        [SerializeField] private ParticleSystem angryEffect;
        [Tooltip("Keep the medicine hand visible while dying (it shows only while drinking otherwise).")]
        [SerializeField] private bool keepHandWhileDying;
        [SerializeField, Min(0f)] private float ghostDelay = .5f;
        [SerializeField, Min(.1f)] private float ghostRiseSeconds = 1.2f;
        [Tooltip("How far below the body the Ghost starts rising, world units.")]
        [SerializeField, Min(0f)] private float ghostRiseDepth = 4f;
        [SerializeField, Min(0)] private int maxExtraGhosts = 3;
        [SerializeField, Min(.5f)] private float ghostSpacing = 2.9f;
        [SerializeField, Min(0f)] private float ghostEnterInterval = .7f;

        [Header("Happy patients")]
        [SerializeField] private GameObject eyeHappy;
        [SerializeField] private GameObject mouthHappy;
        [SerializeField] private ParticleSystem happyEffect;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float textFadeSeconds = 1f;
        [SerializeField, Min(0f)] private float narrationSeconds = 3f;
        [SerializeField, Min(0f)] private float fadeInSeconds = .5f;
        [SerializeField, Min(0f)] private float walkInDelay = .5f;
        [SerializeField, Min(0f)] private float fadeOutSeconds = 1f;
        [Tooltip("Each line stays this long once fully typed.")]
        [SerializeField, Min(0f)] private float lineHoldSeconds = 1.5f;
        [SerializeField, Min(.1f)] private float playerSpeed = 4.5f;
        [SerializeField, Min(.1f)] private float ghostSpeed = 6f;

        [Header("Lines")]
        [Tooltip("Longer lines are broken in two (at the space nearest the middle) so bubbles stay narrow and their tail stays on the speaker.")]
        [SerializeField, Min(8)] private int maxCharactersPerLine = 24;
        [SerializeField] private string playerQuestion = "Have you taken your medicine? How are you feeling?";
        [SerializeField, TextArea] private string patientThanks = "Yes, I have. I've never felt better.\nThank you, Doctor.";
        [SerializeField, TextArea] private string ghostAlone = "That Doctor killed me!\nI will haunt his place!";
        [SerializeField] private string ghostLeader = "That Doctor killed me!";
        [SerializeField] private string ghostReply = "Me too!";
        [SerializeField] private string ghostLeaderEnd = "Let's go to his place!";

        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<Material> skinMaterials = new List<Material>();
        private Dictionary<string, GameObject> worldByName;
        private bool playing;

        public bool IsConfigured => sceneCamera != null && blackCover != null && laterThatNightText != null &&
            playerStart != null && playerStop != null && patientSpots != null && patientSpots.Length > 0 &&
            patientSpots[0] != null && ghostDoor != null && player != null && playerDialogue != null && ghostPrefab != null &&
            eyeHappy != null && mouthHappy != null && happyEffect != null && angryEffect != null;

        private void Awake()
        {
            foreach (var target in hideDuringCutscene)
                if (target != null) target.SetActive(false);
            // The cutscene room is always fully lit: no electricity dimming/flicker and no power warning.
            foreach (var lights in FindObjectsByType<HomePowerLights>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (lights.gameObject.scene == gameObject.scene) lights.IgnoreElectricity();
            SetCover(1f);
            if (laterThatNightText != null) { laterThatNightText.alpha = 0f; laterThatNightText.gameObject.SetActive(false); }
            if (player != null && player.TryGetComponent<Rigidbody2D>(out var body)) body.simulated = false;
            if (player != null && playerStart != null) player.transform.position = playerStart.position;
            // The player's bubble sits under the art that flips with walking direction; keep it unflipped on the
            // left of the head (towards the patients) and its text readable.
            var box = player != null ? player.transform.Find("All/ChatBox") : null;
            if (box != null) { box.SetParent(player.transform, false); RaiseBubble(box); }
        }

        // Own sorting group at the root so the bubble is never hidden behind characters or the foreground.
        private void RaiseBubble(Transform box)
        {
            if (box == null) return;
            if (!box.TryGetComponent<SortingGroup>(out var group)) group = box.gameObject.AddComponent<SortingGroup>();
            group.sortAtRoot = true;
            group.sortingOrder = bubbleSortingOrder;
        }

        // ---------------- Who appears ----------------

        public Task PlayAsync(DayTreatmentResult result, CancellationToken token)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            var cured = new List<GameObject>();
            var dead = new List<GameObject>();
            foreach (var patient in result.Patients)
            {
                if (patient == null || patient.IsJailed) continue; // Correctly reported: taken away, not dead.
                (patient.IsCorrect ? cured : dead).Add(PatientPrefab(patient.NpcId));
            }
            // Older results without per-patient data still get someone on stage.
            for (int i = cured.Count; i < Math.Min(2, result.CorrectPatients) && result.Patients.Count == 0; i++) cured.Add(null);
            for (int i = dead.Count; i < result.IncorrectPatients && result.Patients.Count == 0; i++) dead.Add(null);
            return PlayAsync(cured, dead, token);
        }

        // cured / dead: Office or world prefabs (null = any NPC). Someone dead → the death cutscene.
        internal Task PlayAsync(IReadOnlyList<GameObject> cured, IReadOnlyList<GameObject> dead, CancellationToken token)
        {
            if (!IsConfigured) throw new InvalidOperationException("Endday_Cutscene references are incomplete.");
            if (playing) throw new InvalidOperationException("The end-of-day cutscene is already playing.");
            playing = true;
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var routine = StartCoroutine(Run(dead.Count > 0 ? Death(dead) : Cured(cured), completion));
            token.Register(() =>
            {
                if (this != null) StopCoroutine(routine);
                playing = false;
                completion.TrySetCanceled();
            });
            return completion.Task;
        }

        private IEnumerator Run(IEnumerator body, TaskCompletionSource<bool> completion)
        {
            yield return Narration();
            while (true)
            {
                object step;
                try { if (!body.MoveNext()) break; step = body.Current; }
                catch (Exception exception) { playing = false; completion.TrySetException(exception); yield break; }
                yield return step;
            }
            SetCover(1f);
            playing = false;
            completion.TrySetResult(true);
        }

        private GameObject PatientPrefab(string npcId)
        {
            var definition = npcProfile != null ? npcProfile.Find(npcId) : null;
            return definition != null ? definition.Prefab : null;
        }

        private GameObject WorldPrefab(GameObject prefab)
        {
            if (worldByName == null)
            {
                worldByName = new Dictionary<string, GameObject>(StringComparer.Ordinal);
                var catalog = worldCatalog != null ? worldCatalog : Resources.Load<NpcCatalog>("NpcWorldCatalog");
                if (catalog != null && catalog.prefabs != null)
                    foreach (var world in catalog.prefabs)
                        if (world != null) worldByName[world.name] = world;
            }
            if (prefab != null)
                return worldByName.TryGetValue(prefab.name + WorldSuffix, out var copy) ? copy : prefab;
            // Unknown patient: any walking NPC.
            if (worldByName.Count == 0) return null;
            var all = new List<GameObject>(worldByName.Values);
            return all[Random.Range(0, all.Count)];
        }

        // ---------------- The two cutscenes ----------------

        private IEnumerator Cured(IReadOnlyList<GameObject> cured)
        {
            int count = cured.Count >= 2 && patientSpots.Length >= 2 && patientSpots[1] != null ? Random.Range(1, 3) : 1;
            var picks = new List<GameObject>(cured);
            var patients = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, picks.Count);
                var patient = SpawnPatient(picks.Count > 0 ? picks[index] : null, patientSpots[i]);
                if (picks.Count > 0) picks.RemoveAt(index);
                if (patient != null) patients.Add(patient);
            }
            player.transform.position = playerStart.position;

            yield return Fade(1f, 0f, fadeInSeconds);
            yield return Wait(walkInDelay);
            yield return MoveX(player.transform, playerStop.position.x, playerSpeed);
            yield return null; // Let the run cycle settle to Idle.

            playerDialogue.SpeakFor(Wrap(playerQuestion), lineHoldSeconds, worldLift: dialogueLift);
            yield return WaitWhile(() => playerDialogue.IsShowing);

            if (patients.Count > 0)
            {
                var speaker = patients[Random.Range(0, patients.Count)].GetComponent<NpcDialogue>();
                if (speaker != null)
                {
                    // The speaker (and its bubble) draws over the other patient.
                    if (speaker.TryGetComponent<SortingGroup>(out var front)) front.sortingOrder = characterSortingOrder + 2;
                    SetFace(speaker.gameObject, eyeHappy, mouthHappy);
                    speaker.SpeakFor(Wrap(patientThanks), lineHoldSeconds, animateMouth: false, worldLift: dialogueLift);
                    yield return WaitWhile(() => speaker.IsShowing && !speaker.IsHolding);
                    if (speaker.IsHolding)
                        foreach (var patient in patients) ShowHappy(patient);
                    yield return WaitWhile(() => speaker.IsShowing);
                }
            }
            yield return Fade(0f, 1f, fadeOutSeconds);
            Cleanup();
        }

        private IEnumerator Death(IReadOnlyList<GameObject> dead)
        {
            int victimIndex = Random.Range(0, dead.Count);
            var victim = SpawnPatient(dead[victimIndex], patientSpots[0]);
            player.gameObject.SetActive(false); // The player is not in this scene.

            yield return Fade(1f, 0f, fadeInSeconds);
            if (victim != null) yield return DrinkAndDie(victim);
            yield return Wait(ghostDelay);

            var spot = victim != null ? victim.transform.position : patientSpots[0].position;
            var leader = SpawnGhost(spot + Vector3.down * ghostRiseDepth);
            yield return Rise(leader, spot);

            int extra = Mathf.Min(dead.Count - 1, maxExtraGhosts);
            if (extra <= 0)
            {
                yield return SayAngry(leader, ghostAlone);
                yield return MoveX(leader.transform, OffscreenX(), ghostSpeed);
            }
            else
            {
                // The others float in from the left one after another, pass the first Ghost and line up on its
                // right (inside the frame), so every speech bubble - drawn left of its Ghost - stays on screen.
                var ghosts = new List<GhostDialogue> { leader };
                var arrivals = new List<Coroutine>();
                float door = OffscreenX();
                float depth = Mathf.Abs(spot.z - sceneCamera.transform.position.z);
                float rightLimit = sceneCamera.ViewportToWorldPoint(new Vector3(1f, .5f, depth)).x - ghostSpacing * .5f;
                float step = Mathf.Min(ghostSpacing, Mathf.Max(.5f, (rightLimit - spot.x) / extra));
                for (int i = 1; i <= extra; i++)
                {
                    var ghost = SpawnGhost(new Vector3(door, spot.y, spot.z));
                    ghosts.Add(ghost);
                    arrivals.Add(StartCoroutine(MoveX(ghost.transform, spot.x + step * (extra - i + 1), ghostSpeed)));
                    yield return Wait(ghostEnterInterval);
                }
                foreach (var arrival in arrivals) yield return arrival;

                yield return SayAngry(leader, ghostLeader);
                var other = ghosts[Random.Range(1, ghosts.Count)];
                yield return SayAngry(other, ghostReply);
                yield return SayAngry(leader, ghostLeaderEnd);

                // The leader goes first, the others follow one after another.
                var exits = new List<Coroutine>();
                var order = new List<GhostDialogue>(ghosts);
                order.Sort((x, y) => x.transform.position.x.CompareTo(y.transform.position.x));
                for (int i = 0; i < order.Count; i++)
                {
                    exits.Add(StartCoroutine(MoveX(order[i].transform, door - ghostSpacing * .5f * i, ghostSpeed)));
                    yield return Wait(ghostEnterInterval * .6f);
                }
                foreach (var exit in exits) yield return exit;
            }
            yield return Fade(0f, 1f, fadeOutSeconds);
            Cleanup();
        }

        private IEnumerator DrinkAndDie(GameObject victim)
        {
            var animator = victim.GetComponentInChildren<Animator>();
            var hand = animator != null ? animator.transform.Find("Hand") : null;
            if (animator == null) yield break;
            if (hand != null) hand.gameObject.SetActive(true);
            yield return PlayOnce(animator, DrinkState);
            if (hand != null && !keepHandWhileDying) hand.gameObject.SetActive(false);

            animator.Play(DieState, 0, 0f);
            yield return null;
            SetFace(victim, eyeHurt, mouthHurt);
            var skin = new List<Material>();
            var skinColors = new List<Color>();
            foreach (var sprite in victim.GetComponentsInChildren<SpriteRenderer>(true))
                if (sprite.name == "Head" || sprite.name == "Neck")
                {
                    if (sprite.sharedMaterial == null || !sprite.sharedMaterial.HasProperty("_Color")) continue;
                    // The skin shader uses material color. Give each part its own
                    // instance so the corpse tint cannot affect other patients.
                    var material = new Material(sprite.sharedMaterial);
                    sprite.sharedMaterial = material;
                    skinMaterials.Add(material);
                    skin.Add(material);
                    skinColors.Add(material.color);
                }
            float length = StateLength(animator, DieState);
            float elapsed = 0f;
            bool dying = false;
            // Stop just before the end: a looping clip would wrap back to its first frame at 1.0.
            while (elapsed < Mathf.Max(length - .03f, dyingSkinSeconds))
            {
                if (!dying && elapsed >= hurtFaceSeconds) { SetFace(victim, eyeDie, mouthDie); dying = true; }
                for (int i = 0; i < skin.Count; i++)
                {
                    var target = dyingSkinColor;
                    target.a = skinColors[i].a;
                    skin[i].color = Color.Lerp(skinColors[i], target,
                        dyingSkinSeconds > 0f ? Mathf.Clamp01(elapsed / dyingSkinSeconds) : 1f);
                }
                if (elapsed >= length - .03f) animator.speed = 0f;
                yield return null;
                elapsed += Time.deltaTime;
            }
            for (int i = 0; i < skin.Count; i++)
            {
                var target = dyingSkinColor;
                target.a = skinColors[i].a;
                skin[i].color = target;
            }
            if (!dying) SetFace(victim, eyeDie, mouthDie);
            // Hold the last frame (lying on the floor).
            animator.speed = 0f;
            animator.Play(DieState, 0, .999f);
            animator.Update(0f);
        }

        private IEnumerator PlayOnce(Animator animator, int state)
        {
            animator.Play(state, 0, 0f);
            yield return null;
            yield return Wait(StateLength(animator, state));
        }

        private static float StateLength(Animator animator, int state)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            return info.fullPathHash == state ? info.length / Mathf.Max(.01f, Mathf.Abs(animator.speed * info.speed)) : 0f;
        }

        // ---------------- Characters ----------------

        private GameObject SpawnPatient(GameObject prefab, Transform spot)
        {
            var world = WorldPrefab(prefab);
            if (world == null || spot == null) return null;
            var npc = Instantiate(world, spot.position, Quaternion.identity, transform);
            spawned.Add(npc);
            if (!npc.TryGetComponent<SortingGroup>(out var sorting)) sorting = npc.AddComponent<SortingGroup>();
            sorting.sortingOrder = characterSortingOrder;
            // The examination shader draws one face only; walking scenes use the sprite material.
            if (examinationShader != null && walkingSpriteMaterial != null)
                foreach (var sprite in npc.GetComponentsInChildren<SpriteRenderer>(true))
                    if (sprite.sharedMaterial != null && sprite.sharedMaterial.shader == examinationShader)
                        sprite.sharedMaterial = walkingSpriteMaterial;
            var hand = npc.transform.Find("All/Hand");
            if (hand != null) hand.gameObject.SetActive(false);
            RaiseBubble(npc.transform.Find("ChatBox"));
            var animator = npc.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.Play(IdleState, 0, Random.value);
                if (patientsFaceRight)
                {
                    // Idle keys the animator root's scale every frame. Mirror a
                    // separate parent so animation cannot turn the patient back.
                    var facing = new GameObject("Cutscene Facing").transform;
                    facing.SetParent(animator.transform.parent, false);
                    animator.transform.SetParent(facing, false);
                    facing.localScale = new Vector3(-1f, 1f, 1f);
                }
            }
            return npc;
        }

        private GhostDialogue SpawnGhost(Vector3 position)
        {
            var ghost = Instantiate(ghostPrefab, position, Quaternion.identity, transform);
            spawned.Add(ghost.gameObject);
            if (!ghost.TryGetComponent<SortingGroup>(out var sorting)) sorting = ghost.gameObject.AddComponent<SortingGroup>();
            sorting.sortingOrder = characterSortingOrder + 1;
            if (ghost.ChatBox != null) RaiseBubble(ghost.ChatBox.transform);
            return ghost;
        }

        // Rises from below the floor at the body, fading in.
        private IEnumerator Rise(GhostDialogue ghost, Vector3 target)
        {
            var renderers = ghost.GetComponentsInChildren<SpriteRenderer>(true);
            var colors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) colors[i] = renderers[i].color;
            var start = ghost.transform.position;
            float elapsed = 0f;
            while (elapsed < ghostRiseSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / ghostRiseSeconds);
                float eased = 1f - (1f - t) * (1f - t);
                ghost.transform.position = Vector3.Lerp(start, target, eased);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null || (ghost.ChatBox != null && renderers[i].transform.IsChildOf(ghost.ChatBox.transform))) continue;
                    var c = colors[i]; c.a *= t; renderers[i].color = c;
                }
                yield return null;
            }
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].color = colors[i];
        }

        // Eye/Mouth slots of NpcAppearanceProfile: hide the face part and show the given prefab in its place.
        private static void SetFace(GameObject npc, GameObject eye, GameObject mouth)
        {
            var appearance = npc.GetComponent<NpcAppearanceProfile>();
            if (appearance?.Slots == null) return;
            foreach (var slot in appearance.Slots)
            {
                if (slot?.attachPoint == null) continue;
                var prefab = string.Equals(slot.id, "Eye", StringComparison.OrdinalIgnoreCase) ? eye :
                    string.Equals(slot.id, "Mouth", StringComparison.OrdinalIgnoreCase) ? mouth : null;
                if (prefab == null) continue;
                for (int i = slot.attachPoint.childCount - 1; i >= 0; i--)
                {
                    var child = slot.attachPoint.GetChild(i);
                    if (child.name.StartsWith("Cutscene ", StringComparison.Ordinal)) { Destroy(child.gameObject); continue; }
                    child.gameObject.SetActive(false);
                }
                // The expression prefab keeps its own placement and size on the slot (special looks offset and
                // scale their own eyes/mouths, which must not be copied onto the standard Hurt/Die parts).
                var face = Instantiate(prefab, slot.attachPoint, false);
                face.name = "Cutscene " + prefab.name;
            }
        }

        private void ShowHappy(GameObject patient)
        {
            if (happyEffect == null || patient == null) return;
            var profile = patient.GetComponent<PatientExaminationProfile>();
            var position = profile != null && profile.Heart != null ? profile.Heart.position : patient.transform.position;
            PlayEmotion(happyEffect, position, transform);
        }

        private IEnumerator SayAngry(GhostDialogue ghost, string line)
        {
            ghost.Say(Wrap(line), lineHoldSeconds);
            if (ghost.IsSpeaking)
            {
                // Use the visible body, excluding the speech bubble and any earlier effects.
                var body = ghost.GetComponent<SpriteRenderer>();
                if (body == null)
                    foreach (var sprite in ghost.GetComponentsInChildren<SpriteRenderer>())
                        if (ghost.ChatBox == null || !sprite.transform.IsChildOf(ghost.ChatBox.transform))
                        { body = sprite; break; }
                PlayEmotion(angryEffect, body != null ? body.bounds.center : ghost.transform.position, ghost.transform);
            }
            yield return WaitWhile(() => ghost.IsSpeaking);
        }

        private void PlayEmotion(ParticleSystem prefab, Vector3 position, Transform parent)
        {
            if (prefab == null) return;
            var effect = Instantiate(prefab, position, prefab.transform.rotation, transform);
            effect.transform.SetParent(parent, true);
            spawned.Add(effect.gameObject);
            foreach (var system in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = system.main;
                main.loop = false;
                main.ringBufferMode = ParticleSystemRingBufferMode.Disabled;
            }
            foreach (var renderer in effect.GetComponentsInChildren<ParticleSystemRenderer>(true))
                renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, characterSortingOrder + 3);
            effect.Play(true);
        }

        // ---------------- Helpers ----------------

        // Breaks every line longer than maxCharactersPerLine at the space nearest its middle (repeatedly).
        private string Wrap(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var result = new System.Text.StringBuilder();
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) result.Append('\n');
                AppendWrapped(result, lines[i].Trim());
            }
            return result.ToString();
        }

        private void AppendWrapped(System.Text.StringBuilder result, string line)
        {
            if (line.Length <= maxCharactersPerLine) { result.Append(line); return; }
            int middle = line.Length / 2, split = -1;
            for (int offset = 0; offset <= middle; offset++)
            {
                if (middle + offset < line.Length && line[middle + offset] == ' ') { split = middle + offset; break; }
                if (middle - offset > 0 && line[middle - offset] == ' ') { split = middle - offset; break; }
            }
            if (split <= 0) split = maxCharactersPerLine;
            AppendWrapped(result, line.Substring(0, split).TrimEnd());
            result.Append('\n');
            AppendWrapped(result, line.Substring(split + (line[split] == ' ' ? 1 : 0)).TrimStart());
        }

        private IEnumerator Narration()
        {
            SetCover(1f);
            var text = laterThatNightText;
            text.alpha = 0f;
            text.gameObject.SetActive(true);
            float fade = textFadeSeconds;
            float total = narrationSeconds + 2f * textFadeSeconds;
            float elapsed = 0f;
            while (elapsed < total)
            {
                yield return null;
                elapsed += Time.deltaTime;
                text.alpha = fade > 0f ? Mathf.Clamp01(Mathf.Min(elapsed, total - elapsed) / fade) : 1f;
            }
            text.alpha = 0f;
            text.gameObject.SetActive(false);
        }

        private float OffscreenX()
        {
            // Frame edge at the Ghosts' depth (works for a perspective or orthographic camera).
            float depth = Mathf.Abs(ghostDoor.position.z - sceneCamera.transform.position.z);
            bool left = ghostDoor.position.x < sceneCamera.transform.position.x;
            float edge = sceneCamera.ViewportToWorldPoint(new Vector3(left ? 0f : 1f, .5f, depth)).x;
            // The door marker, unless it is still inside the frame.
            return left ? Mathf.Min(ghostDoor.position.x, edge - 3f) : Mathf.Max(ghostDoor.position.x, edge + 3f);
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            float elapsed = 0f;
            SetCover(from);
            while (elapsed < seconds)
            {
                yield return null;
                elapsed += Time.deltaTime;
                SetCover(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / seconds)));
            }
            SetCover(to);
        }

        private void SetCover(float alpha)
        {
            if (blackCover == null) return;
            blackCover.alpha = alpha;
            blackCover.blocksRaycasts = alpha > 0f;
        }

        private static IEnumerator MoveX(Transform target, float x, float speed)
        {
            while (target != null && Mathf.Abs(target.position.x - x) > .001f)
            {
                var position = target.position;
                position.x = Mathf.MoveTowards(position.x, x, speed * Time.deltaTime);
                target.position = position;
                yield return null;
            }
        }

        private static IEnumerator Wait(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds) { yield return null; elapsed += Time.deltaTime; }
        }

        private static IEnumerator WaitWhile(Func<bool> condition)
        {
            while (condition()) yield return null;
        }

        private void Cleanup()
        {
            foreach (var go in spawned) if (go != null) Destroy(go);
            spawned.Clear();
            foreach (var material in skinMaterials) if (material != null) Destroy(material);
            skinMaterials.Clear();
        }

        private void OnDisable()
        {
            playing = false;
        }

        private void OnDestroy() => Cleanup();
    }
}
