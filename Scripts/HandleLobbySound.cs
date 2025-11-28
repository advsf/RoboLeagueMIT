using UnityEngine;
using System.Collections;

public class HandleLobbySound : MonoBehaviour
{
    public static HandleLobbySound instance;

    [Header("Music References")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip music;
    [SerializeField] private float delayBeforePlaying;


    private void Start()
    {
        // singleton design
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // also fade in the music in the beginning with delay
            StartCoroutine(FadeInMusic(delayBeforePlaying, 3));
        }

        else
        {
            Destroy(gameObject);
            StartCoroutine(FadeInMusic(0, 3));
        }
    }

    public IEnumerator FadeInMusic(float delay, float fadeDuration)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log("called!");

        musicSource.Play();

        float time = Time.time;

        while (Time.time - time <= fadeDuration)
        {
            musicSource.volume = (Time.time - time) / fadeDuration;
            yield return null;
        }
    }

    public IEnumerator FadeOutMusic(float delay, float fadeDuration)
    {
        yield return new WaitForSeconds(delay);

        float time = Time.time;

        while (Time.time - time <= fadeDuration)
        {
            musicSource.volume = 1 - ((Time.time - time) / fadeDuration);
            yield return null;
        }

        musicSource.Stop();
    }
}
