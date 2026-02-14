using System.Collections.Generic;
using UnityEngine;

public class LevelStreamGenerator : MonoBehaviour
{
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
        if (player == null || database == null || database.segments.Count == 0) return;

        // Garante que sempre tem segments suficientes na frente
        while (NeedMoreAhead())
            SpawnNext();

        // Despawn atrás
        DespawnBehind();
    }

    private void BootstrapInitial()
    {
        // Zera tudo
        while (_spawned.Count > 0)
        {
            var s = _spawned.Dequeue();
            if (s != null) Destroy(s.gameObject);
        }

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

        // Tenta pegar Start
        var startSeg = PickSegment(preferTag: SegmentTag.Start);

        if (startSeg == null)
        {
            Debug.LogWarning("[LevelStreamGenerator] Nenhum segment com tag Start encontrado. Usando o primeiro da lista como fallback.");
            startSeg = database.segments[0];
        }

        Debug.Log($"[LevelStreamGenerator] StartSeg = {startSeg.name}");

        // Spawna o primeiro
        var first = SpawnAt(startSeg, _nextSpawnWorldPos);
        _last = first;

        if (first == null)
        {
            Debug.LogError("[LevelStreamGenerator] first ficou NULL após SpawnAt. (prefab startSeg estava NULL?)");
            return;
        }

        // ✅ Spawn do player IMEDIATAMENTE após criar o first
        if (player == null)
        {
            Debug.LogError("[LevelStreamGenerator] Player Transform está NULL. Confere Tag Player ou arraste no inspector.");
            return;
        }

        Transform spawn = first.playerSpawn != null ? first.playerSpawn : first.startAnchor;

        if (spawn == null)
        {
            Debug.LogError($"[LevelStreamGenerator] O segment '{first.name}' não tem playerSpawn e nem startAnchor preenchidos no SegmentSpec.");
            return;
        }

        PlacePlayerAt(spawn.position);

        Debug.Log($"[LevelStreamGenerator] Player spawnado em {spawn.position} | first={first.name} | playerSpawn={(first.playerSpawn != null)} startAnchor={(first.startAnchor != null)}");

        // Agora sim atualiza next e gera o resto
        _nextSpawnWorldPos = GetNextSpawnPos(first);

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
        // Heurística simples: alterna tags pra não ficar chato
        var seg = PickSegmentSmart();
        var spawned = SpawnAt(seg, _nextSpawnWorldPos);
        _last = spawned;
        _nextSpawnWorldPos = GetNextSpawnPos(spawned);

        _spawned.Enqueue(spawned);
    }

    private SegmentSpec PickSegmentSmart()
    {
        // Você pode sofisticar depois. Por agora, evita repetir exatamente o mesmo prefab.
        SegmentTag prefer = SegmentTag.Neutral;

        int roll = _rng.Next(0, 100);
        if (roll < 20) prefer = SegmentTag.Precision;
        else if (roll < 35) prefer = SegmentTag.Hazard;
        else if (roll < 45) prefer = SegmentTag.Treasure;
        else if (roll < 55) prefer = SegmentTag.Vertical;
        else prefer = SegmentTag.Neutral;

        var picked = PickSegment(prefer);

        // fallback
        if (picked == null) picked = database.segments[_rng.Next(database.segments.Count)];
        return picked;
    }

    private SegmentSpec PickSegment(SegmentTag preferTag)
    {
        // filtra por tag
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

    private SegmentSpec SpawnAt(SegmentSpec prefab, Vector3 worldPos)
    {
        if (prefab == null) return null;

        var inst = Instantiate(prefab, worldPos, Quaternion.identity, transform);

        // Alinha StartAnchor do inst exatamente no ponto de spawn
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
        // próximo ponto = EndAnchor do segmento atual
        return current.endAnchor != null ? current.endAnchor.position : current.transform.position + Vector3.right * 10f;
    }

    private void DespawnBehind()
    {
        // Remove segments muito atrás do player, mantendo um buffer
        while (_spawned.Count > 0)
        {
            var oldest = _spawned.Peek();
            if (oldest == null)
            {
                _spawned.Dequeue();
                continue;
            }

            float behind = player.position.x - oldest.transform.position.x;
            if (behind < despawnBehindDistance) break;

            // Mantém alguns atrás
            if (_spawned.Count <= keepSegmentsBehind + keepSegmentsAhead) break;

            _spawned.Dequeue();
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

            // evita qualquer desync visual
            player.position = worldPos;
        }
        else
        {
            player.position = worldPos;
        }

        var pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.SetSpawnPosition(worldPos);
        }
    }
}
