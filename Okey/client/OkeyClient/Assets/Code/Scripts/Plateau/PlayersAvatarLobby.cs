using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // This import might not be needed if you're only using SpriteRenderer

public class PlayerAvatarsLobby : MonoBehaviour
{
    public static PlayerAvatarsLobby Instance { get; private set; }

    public SpriteRenderer mainPlayerRenderer;
    public SpriteRenderer player2Renderer;
    public SpriteRenderer player3Renderer;
    public SpriteRenderer player4Renderer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        DontDestroyOnLoad(this.gameObject);
    }

    public void LoadAndDisplayAvatars(List<Sprite> avatars)
    {
        if (avatars.Count > 0)
        {
            mainPlayerRenderer.sprite = avatars[0];

            if (avatars.Count > 1)
                player2Renderer.sprite = avatars[1];

            if (avatars.Count > 2)
                player3Renderer.sprite = avatars[2];

            if (avatars.Count > 3)
                player4Renderer.sprite = avatars[3];
        }
    }
}
