using System.Collections.Generic;
using UnityEngine;

public class LevelStreamGenerator : MonoBehaviour
{
    // Gate numbering: Start=1, depois 2,3,4...
    private readonly Queue<int> _spawnedGateNumbers = new();
    private int _nextGateNumberToSpawn = 1;

    [Header("Refs")]
    [SerializeField] private SegmentDatabase database;
    [SerializeField] private Transform player;

    [Header("Streaming")]
    [SerializeField] private int keepSegmentsAhead = 8;
    [SerializeField] private int keepSegmentsBehind = 2;
    [SerializeField] private float spawnAheadDistance = 25f;
    [SerializeField] private float despawnBehindDistance = 35f;

    [Header("Seed")]
    [SerializeField] private bool useRunManagerSeed = true;

    private readonly Queue<SegmentSpec> _spawned = new();
    private System.Random _rng;

    private Vector3 _nextSpawnWorldPos;
    private SegmentSpec _last;

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        int seed = useRunManagerSeed && RunManager.I != null ? RunManager.I.Seed : 12345;
        _rng = new System.Random(seed);

        BootstrapInitial();
    }

    private void Update()
    {
        if (player == null || database == null || database.segments == null || database.segments.Count == 0)
            return;

        while (NeedMoreAhead())
            SpawnNext();

        DespawnBehind();
    }

    private void BootstrapInitial()
    {
        // Reset total
        while (_spawned.Count > 0)
        {
            var s = _spawned.Dequeue();
            if (s != null) Destroy(s.gameObject);
        }
        _spawnedGateNumbers.Clear();

        _nextSpawnWorldPos = transform.position;
        _last = null;

        if (database == null)
        {
            Debug.LogError("[LevelStreamGenerator] SegmentDatabase está NULL no inspector.");
            return;
        }

        if (database.segments == null || database.segments.Count == 0)
        {
            Debug.LogError("[LevelStreamGenerator] SegmentDatabase não tem segments.");
            return;
        }

        // Pick Start (pelo SegmentSpec.tag)
        var startSeg = PickSegment(SegmentTag.Start);

        if (startSeg == null)
        {
            Debug.LogWarning("[LevelStreamGenerator] Nenhum segment com tag Start encontrado. Usando o primeiro da lista como fallback.");
            startSeg = database.segments[0];
        }

        Debug.Log($"[LevelStreamGenerator] StartSeg = {startSeg.name}");

        // Spawna Start (gate 1)
        var first = SpawnAt(startSeg, _nextSpawnWorldPos);
        _last = first;

        if (first == null)
        {
            Debug.LogError("[LevelStreamGenerator] first ficou NULL após SpawnAt. (prefab startSeg estava NULL?)");
            return;
        }

        // Próximo gate a spawnar é o 2
        _nextGateNumberToSpawn = 2;

        // Spawn player
        if (player == null)
        {
            Debug.LogError("[LevelStreamGenerator] Player Transform está NULL.");
            return;
        }

        Transform spawn = first.playerSpawn != null ? first.playerSpawn : first.startAnchor;
        if (spawn == null)
        {
            Debug.LogError($"[LevelStreamGenerator] Segment '{first.name}' sem playerSpawn e sem startAnchor.");
            return;
        }

        PlacePlayerAt(spawn.position);

        // Próximo ponto de spawn
        _nextSpawnWorldPos = GetNextSpawnPos(first);

        // Pre-spawn inicial
        for (int i = 0; i < keepSegmentsAhead; i++)
            SpawnNext();
    }

    private bool NeedMoreAhead()
    {
        if (_spawned.Count < keepSegmentsAhead + keepSegmentsBehind) return true;

        float dist = _nextSpawnWorldPos.x - player.position.x;
        return dist < spawnAheadDistance;
    }

    private void SpawnNext()
    {
        int gateNumber = _nextGateNumberToSpawn;

        var seg = PickPlannedSegment(gateNumber);
        if (seg == null)
        {
            Debug.LogError("[LevelStreamGenerator] PickPlannedSegment retornou NULL. Usando fallback.");
            seg = SafeFallbackNonStartNonEnd();
        }

        var spawned = SpawnAt(seg, _nextSpawnWorldPos);

        _last = spawned;
        _nextSpawnWorldPos = GetNextSpawnPos(spawned);

        _spawned.Enqueue(spawned);
        _spawnedGateNumbers.Enqueue(gateNumber);

        _nextGateNumberToSpawn++;

        // Debug opcional:
        // Debug.Log($"[SpawnNext] gate={gateNumber} tag={seg.tag} name={seg.name}");
    }

    /// <summary>
    /// Regra:
    /// - Sempre começa em Start (gate 1).
    /// - Infinity: End em 5, 10, 15... (sempre o 5º gate de cada bloco).
    /// - Modos finitos: End só no último gate (mode.maxStages).
    /// - Fora desses casos: nunca Start/End, só mids.
    /// </summary>
    private SegmentSpec PickPlannedSegment(int gateNumber)
    {
        if (RunManager.I == null || RunManager.I.CurrentMode == null)
            return SafeFallbackNonStartNonEnd();

        var mode = RunManager.I.CurrentMode;
        bool isInfinity = mode.type == GameModeType.Infinity;

        // ✅ Infinity: End a cada 5 gates (5,10,15...)
        if (isInfinity)
        {
            if (gateNumber % 5 == 0)
            {
                var end = PickSegmentStrict(SegmentTag.End);
                if (end != null) return end;

                Debug.LogWarning("[LevelStreamGenerator] Era pra spawnar End (Infinity), mas não há End no DB. Usando fallback.");
                return SafeFallbackNonStartNonEnd();
            }
        }
        else
        {
            // ✅ Finito: End só no último gate
            if (gateNumber == mode.maxStages)
            {
                var end = PickSegmentStrict(SegmentTag.End);
                if (end != null) return end;

                Debug.LogWarning("[LevelStreamGenerator] Era pra spawnar End final, mas não há End no DB. Usando fallback.");
                return SafeFallbackNonStartNonEnd();
            }
        }

        // Mid: nunca Start/End
        return PickSegmentSmartNonStartNonEnd() ?? SafeFallbackNonStartNonEnd();
    }

    private SegmentSpec PickSegmentSmartNonStartNonEnd()
    {
        SegmentTag prefer;

        int roll = _rng.Next(0, 100);
        if (roll < 20) prefer = SegmentTag.Precision;
        else if (roll < 35) prefer = SegmentTag.Hazard;
        else if (roll < 45) prefer = SegmentTag.Treasure;
        else if (roll < 55) prefer = SegmentTag.Vertical;
        else prefer = SegmentTag.Neutral;

        // tenta preferida
        var picked = PickSegment(prefer);

        // bloqueia Start/End por segurança
        if (picked != null && (picked.tag == SegmentTag.Start || picked.tag == SegmentTag.End))
            picked = null;

        if (picked != null) return picked;

        // fallback: pega algo não Start/End
        for (int i = 0; i < 30; i++)
        {
            var s = database.segments[_rng.Next(database.segments.Count)];
            if (s == null) continue;
            if (s.tag == SegmentTag.Start || s.tag == SegmentTag.End) continue;
            if (_last != null && s.name == _last.name) continue;
            return s;
        }

        return null;
    }

    private SegmentSpec PickSegment(SegmentTag preferTag)
    {
        List<SegmentSpec> pool = null;
        foreach (var s in database.segments)
        {
            if (s == null) continue;
            if (s.tag != preferTag) continue;
            if (_last != null && s.name == _last.name) continue;

            pool ??= new List<SegmentSpec>();
            pool.Add(s);
        }

        if (pool == null || pool.Count == 0) return null;
        return pool[_rng.Next(pool.Count)];
    }

    private SegmentSpec PickSegmentStrict(SegmentTag preferTag)
    {
        // Igual PickSegment, mas sem qualquer “guard rail” baseado em _last,
        // pra não quebrar End forçado.
        List<SegmentSpec> pool = null;
        foreach (var s in database.segments)
        {
            if (s == null) continue;
            if (s.tag != preferTag) continue;
            if (_last != null && s.name == _last.name) continue;

            pool ??= new List<SegmentSpec>();
            pool.Add(s);
        }

        if (pool == null || pool.Count == 0) return null;
        return pool[_rng.Next(pool.Count)];
    }

    private SegmentSpec PickAnyNonStartNonEnd()
    {
        for (int i = 0; i < 80; i++)
        {
            var s = database.segments[_rng.Next(database.segments.Count)];
            if (s == null) continue;
            if (s.tag == SegmentTag.Start || s.tag == SegmentTag.End) continue;
            if (_last != null && s.name == _last.name) continue;
            return s;
        }
        return null;
    }

    private SegmentSpec SafeFallbackNonStartNonEnd()
    {
        var s = PickAnyNonStartNonEnd();
        if (s != null) return s;

        Debug.LogError("[LevelStreamGenerator] NÃO existe nenhum segment não-Start/não-End no SegmentDatabase!");
        return database.segments[0];
    }

    private SegmentSpec SpawnAt(SegmentSpec prefab, Vector3 worldPos)
    {
        if (prefab == null) return null;

        var inst = Instantiate(prefab, worldPos, Quaternion.identity, transform);

        if (inst.startAnchor != null)
        {
            Vector3 delta = worldPos - inst.startAnchor.position;
            inst.transform.position += delta;
        }
        else
        {
            inst.transform.position = worldPos;
        }

        return inst;
    }

    private Vector3 GetNextSpawnPos(SegmentSpec current)
    {
        if (current == null) return _nextSpawnWorldPos + Vector3.right * 10f;
        return current.endAnchor != null ? current.endAnchor.position : current.transform.position + Vector3.right * 10f;
    }

    private void DespawnBehind()
    {
        while (_spawned.Count > 0)
        {
            var oldest = _spawned.Peek();
            if (oldest == null)
            {
                _spawned.Dequeue();
                if (_spawnedGateNumbers.Count > 0) _spawnedGateNumbers.Dequeue();
                continue;
            }

            float behind = player.position.x - oldest.transform.position.x;
            if (behind < despawnBehindDistance) break;

            if (_spawned.Count <= keepSegmentsBehind + keepSegmentsAhead) break;

            _spawned.Dequeue();
            if (_spawnedGateNumbers.Count > 0) _spawnedGateNumbers.Dequeue();

            Destroy(oldest.gameObject);
        }
    }

    private void PlacePlayerAt(Vector3 worldPos)
    {
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = worldPos;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            player.position = worldPos;
        }
        else
        {
            player.position = worldPos;
        }

        var pc = player.GetComponent<PlayerController>();
        if (pc != null)
            pc.SetSpawnPosition(worldPos);
    }
}