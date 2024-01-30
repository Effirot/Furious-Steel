using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuView : VisualElement {
    public new class UxmlFactory : UxmlFactory<MainMenuView> {}

    private Button StartButton => this.Q<Button>("start-button");

    public MainMenuView() {
        var asset = Resources.Load<VisualTreeAsset>($"Views/{nameof(MainMenuView)}");
        asset.CloneTree(this);

        StartButton.RegisterCallback<ClickEvent>(StartGame);
    }

    ~MainMenuView() {
        StartButton.UnregisterCallback<ClickEvent>(StartGame);
    }

    private void StartGame(ClickEvent evt) {
        SceneManager.LoadScene("MainScene_Enviroment");
        SceneManager.LoadScene("MainScene", LoadSceneMode.Additive);
    }
}
