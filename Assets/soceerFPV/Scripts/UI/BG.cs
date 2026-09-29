using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//*****************************************
//创建人：
//功能说明：菜单界面背景滚动动画和背景音乐控制
//***************************************** 
public class BG : MonoBehaviour
{
    [SerializeField] private RawImage _img;
    [SerializeField] private float _x, _y;
    [SerializeField] private AudioClip _backgroundMusic; 
    private AudioSource _audioSource;

    private void Start()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.clip = _backgroundMusic;
        _audioSource.loop = true; 
        _audioSource.playOnAwake = false; 
    }

    // Update is called once per frame
    void Update()
    {
        _img.uvRect = new Rect(_img.uvRect.position + new Vector2(_x, _y) * Time.deltaTime, _img.uvRect.size);

        if (gameObject.activeInHierarchy && !_audioSource.isPlaying)
        {
            _audioSource.Play(); 
        }
        else if (!gameObject.activeInHierarchy && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }
}