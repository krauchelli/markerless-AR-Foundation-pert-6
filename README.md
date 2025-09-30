# AR Furniture Placement Project (Interactive Room Layout)

This project is a mini assignment for the 6th week of the AR and VR course, designed to fulfill the requirements of the subject. It is a Unity project developed to simulate an interior design application using Augmented Reality. Users can select various types of furniture from the user interface (UI) and place them in the real-world environment through their phone camera.

The app is built with a modern "Select-Preview-Position-Lock" workflow, giving users full control before permanently placing objects.

---

## ✨ Main Features

-   **AR Surface Detection:** Detects horizontal surfaces (floors, tables) in real-time using AR Foundation.
-   **Custom Visualizer:** Uses a custom visualizer with a grid material to clearly represent the detected area.
-   **Furniture Selection via UI:** A dynamic and scalable furniture list is displayed in the UI, allowing users to choose objects to place.
-   **Real-time Preview Mode:** Once selected, furniture appears in "preview" mode, following the camera's view and snapping to detected surfaces.
-   **Live Object Manipulation:** Users can adjust the **scale** and **rotation** of objects in real-time using UI sliders while in preview mode.
-   **Visual Collision Feedback:** The preview object changes color (green/red) to indicate whether the current position is valid or already occupied by another object.
-   **Anti-Collision Logic:** The "Lock Position" functionality performs a final check to prevent objects from overlapping.
-   **UI Notification System:** Feedback messages are displayed on screen to inform users about action statuses (e.g., "Position locked successfully!", "Area already occupied!").
-   **"Clear All" Feature:** A button to remove all placed furniture and reset the design session.
-   **(In Development) App Modes:** A state-based system separating "Mapping Mode" for area calibration and "Placement Mode" for placing furniture.

---

## 🛠️ Technology & Assets

-   **Engine:** Unity 6000.2.1f1 (or newer)
-   **Platform:** Android (with ARCore)
-   **Main Packages:**
    -   AR Foundation (v6.0+)
    -   ARCore XR Plugin
    -   Input System Package
    -   TextMeshPro
-   **3D Assets:**
    -   [Furniture FREE](https://assetstore.unity.com/packages/3d/props/furniture-free-177192) by `ithappy`

---

## 🚀 How to Run

1.  *Clone* this repository.
2.  Open the project using the appropriate Unity version.
3.  Ensure all required packages are installed via `Window > Package Manager`.
4.  Go to `File > Build Settings` and switch the platform to **Android**.
5.  Open `Edit > Project Settings > Player` and set a unique **Package Name** (e.g., `com.yourname.ardenah`).
6.  Make sure your Android device is connected and ARCore compatible.
7.  Click `Build and Run`.

---

## 📝 Project Status

Core functionality for selection, preview, manipulation, and placement of furniture with anti-collision is complete.

**Next Steps:** Full implementation of **Mapping Mode** to allow users to calibrate/measure the design area before starting the furniture placement process.