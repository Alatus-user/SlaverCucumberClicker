using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using UnityEngine;

using PlayerSaveOptions =
    Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

using PlayerLoadOptions =
    Unity.Services.CloudSave.Models.Data.Player.LoadOptions;

public static class PlayerProfileService
{
    private const string AvatarKey = "avatar";
    private const int AvatarSize = 128;

    public static async Task SaveAvatarAsync(Texture2D sourceTexture)
    {
        if (sourceTexture == null)
        { 
            return; 
        }
        Texture2D avatarTexture = 
            CreateReadableTexture(
                sourceTexture,
                AvatarSize,
                AvatarSize
            );

        byte[] imageBytes = avatarTexture.EncodeToJPG(70);

        string base64 = Convert.ToBase64String(imageBytes);

        var data = new Dictionary<string, object>
        {
            { AvatarKey, base64 }
        };

        await CloudSaveService.Instance.Data.Player.SaveAsync(
            data,
            new PlayerSaveOptions( new PublicWriteAccessClassOptions())
        );

        UnityEngine.Object.Destroy(avatarTexture);

        Debug.Log($"Avatar saved: {imageBytes.Length / 1024f:F2} KB");
    }
    private static Texture2D CreateReadableTexture(Texture source, int width, int height)
    {
        RenderTexture renderTexture =
            RenderTexture.GetTemporary(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32
            );

        RenderTexture previous = RenderTexture.active;
        Graphics.Blit(source, renderTexture);
        RenderTexture.active = renderTexture;

        Texture2D readableTexture =
            new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false
            );

        readableTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0 );
        readableTexture.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        return readableTexture;
    }

    public static async Task<Texture2D> LoadAvatarAsync(string playerId)
    {
        var keys = new HashSet<string>
        {
            AvatarKey
        };

        var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
                keys,
                new LoadOptions(new PublicReadAccessClassOptions(playerId))
            );

        if (!result.TryGetValue(AvatarKey, out var avatarData))
        {
            return null;
        }

        string base64 = avatarData.Value.GetAs<string>();

        if (string.IsNullOrEmpty(base64))
        { 
            return null; 
        }

        byte[] imageBytes = Convert.FromBase64String(base64);

        Texture2D texture = new Texture2D(2, 2);

        if (!texture.LoadImage(imageBytes))
        {
            UnityEngine.Object.Destroy(texture);
            return null;
        }

        return texture;
    }
}
 