using STS2Bridge.Integration;

namespace STS2Bridge.Tests.Integration;

public sealed class ExternalImWebSocketClientTests
{
    [Fact]
    public void HandleIncomingMessage_should_mark_logged_in_when_login_succeeds()
    {
        var client = new ExternalImWebSocketClient();

        client.HandleIncomingMessage("""
        {
          "type": "loginResult",
          "success": true,
          "message": "IM 登录成功",
          "data": {
            "userId": "123456",
            "uid": "game_123456"
          }
        }
        """);

        Assert.Equal(ExternalImConnectionState.LoggedIn, client.Status.ConnectionState);
        Assert.Equal("123456", client.Status.CurrentUserId);
        Assert.Equal("game_123456", client.Status.CurrentUid);
        Assert.Null(client.Status.LastError);
    }

    [Fact]
    public void HandleIncomingMessage_should_mark_login_failed_when_login_fails()
    {
        var client = new ExternalImWebSocketClient();

        client.HandleIncomingMessage("""
        {
          "type": "loginResult",
          "success": false,
          "message": "获取 IM 签名失败"
        }
        """);

        Assert.Equal(ExternalImConnectionState.LoginFailed, client.Status.ConnectionState);
        Assert.Equal("获取 IM 签名失败", client.Status.LastError);
    }

    [Fact]
    public void HandleIncomingMessage_should_clear_user_when_logout_succeeds()
    {
        var client = new ExternalImWebSocketClient();
        client.HandleIncomingMessage("""
        {
          "type": "loginResult",
          "success": true,
          "data": {
            "userId": "123456",
            "uid": "game_123456"
          }
        }
        """);

        client.HandleIncomingMessage("""
        {
          "type": "logoutResult",
          "success": true,
          "message": "登出成功"
        }
        """);

        Assert.Equal(ExternalImConnectionState.Connected, client.Status.ConnectionState);
        Assert.Null(client.Status.CurrentUserId);
        Assert.Null(client.Status.LastError);
    }

    [Fact]
    public void HandleIncomingMessage_should_capture_error_message()
    {
        var client = new ExternalImWebSocketClient();

        client.HandleIncomingMessage("""
        {
          "type": "error",
          "message": "连接异常"
        }
        """);

        Assert.Equal(ExternalImConnectionState.Error, client.Status.ConnectionState);
        Assert.Equal("连接异常", client.Status.LastError);
    }
}
