namespace EventNote.Main.Local.Services;

/// <summary>ViewModel 이 Application 을 직접 건드리지 않도록 감싼 앱 수명 제어.</summary>
public interface IAppLifetime
{
    /// <summary>
    /// 앱을 곧바로 끝낸다. 창의 닫기 확인은 거치지 않는다.
    /// 업데이트 설치처럼 이미 저장까지 마치고 확인을 받은 뒤에만 부른다.
    /// </summary>
    void Shutdown();
}
