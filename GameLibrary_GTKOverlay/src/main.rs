use futures_util::stream::StreamExt;
use gio::prelude::*;
use gtk::{gdk_pixbuf::Pixbuf, prelude::*};
use std::collections::HashMap;
use zbus::Connection;

fn app_startup(application: &gtk::Application) {
    let window = gtk::ApplicationWindow::new(application);
    window.set_size_request(100, 40);

    let is_gnome: bool = get_is_gnome();

    if (is_gnome) {
        init_ubuntu(&window);
    } else {
        init_wayland(&window);
    }

    window.set_decorated(false);
    window.set_app_paintable(true);

    // --- controls ---

    let window_close_clone = window.clone();
    let vbox = gtk::Box::new(gtk::Orientation::Horizontal, 5);

    let close_button = gtk::Button::with_label("Close");
    close_button.connect_clicked(move |_| window_close_clone.close());

    let window_screenshot_clone = window.clone();
    let screenshot_button = gtk::Button::with_label("Screenshot");

    screenshot_button.connect_clicked(move |_| {
        let window = window_screenshot_clone.clone();

        glib::MainContext::default().spawn_local(async move {
            if let Err(e) = screenshot_process(&window, is_gnome).await {
                eprintln!("Screenshot failed: {}", e);
                window.close();
            }
        });
    });

    vbox.add(&close_button);
    vbox.add(&screenshot_button);

    window.set_child(Some(&vbox));
    window.show_all();
    window.present();
}

fn get_is_gnome() -> bool {
    std::env::var("XDG_CURRENT_DESKTOP")
        .map(|v| v.to_lowercase().contains("gnome"))
        .unwrap_or(false)
}

fn init_wayland(window: &gtk::ApplicationWindow) {
    gtk_layer_shell::init_for_window(window);

    gtk_layer_shell::set_layer(window, gtk_layer_shell::Layer::Overlay);
    gtk_layer_shell::set_namespace(window, "nexx");
    gtk_layer_shell::auto_exclusive_zone_enable(window);
    gtk_layer_shell::set_margin(window, gtk_layer_shell::Edge::Left, 10);
    gtk_layer_shell::set_margin(window, gtk_layer_shell::Edge::Top, 10);
    gtk_layer_shell::set_anchor(window, gtk_layer_shell::Edge::Left, true);
    gtk_layer_shell::set_anchor(window, gtk_layer_shell::Edge::Top, true);
}

fn init_ubuntu(window: &gtk::ApplicationWindow) {
    window.set_type_hint(gtk::gdk::WindowTypeHint::Dialog);
    window.set_keep_above(true);
    window.connect_realize(|w| {
        w.move_(10, 10);
    });
}

fn get_screenshot_from_clipboard() -> Option<Pixbuf> {
    let clipboard = gtk::Clipboard::get(&gtk::gdk::SELECTION_CLIPBOARD);
    clipboard.wait_for_image()
}

async fn screenshot_process(window: &gtk::ApplicationWindow, is_gnome: bool) -> zbus::Result<()> {
    let connection = Connection::session().await?;

    let proxy = zbus::Proxy::new(
        &connection,
        "org.freedesktop.portal.Desktop",
        "/org/freedesktop/portal/desktop",
        "org.freedesktop.portal.Screenshot",
    )
    .await?;

    let mut options: HashMap<&str, zvariant::OwnedValue> = HashMap::new();

    options.insert("modal", zvariant::OwnedValue::from(true));
    options.insert("interactive", zvariant::OwnedValue::from(true));

    if (is_gnome) {
        let (_handle_path,): (zvariant::OwnedObjectPath,) =
            proxy.call("Screenshot", &("", options)).await?;

        glib::timeout_future_seconds(5).await;

        if let Some(pixbuf) = get_screenshot_from_clipboard() {
            let path = "/tmp/screenshot.png";
            pixbuf.savev(path, "png", &[]).unwrap();
            println!("GTKOVERLAY_RETURNPATH: {}", path);
        } else {
            eprintln!("No image in clipboard");
        }
    } else {
        let (_handle_path,): (zvariant::OwnedObjectPath,) =
            proxy.call("Screenshot", &("", options)).await?;

        let handle_path = _handle_path.to_string();
        let request_proxy = zbus::Proxy::new(
            &connection,
            "org.freedesktop.portal.Desktop",
            handle_path,
            "org.freedesktop.portal.Request",
        )
        .await?;

        glib::MainContext::default().spawn_local(async move {
            let mut __stream__ = match request_proxy.receive_signal("Response").await {
                Ok(s) => s,
                Err(e) => {
                    eprintln!("Failed to receive signal: {}", e);
                    gtk::main_quit();
                    return;
                }
            };

            if let Some(msg) = __stream__.next().await {
                let body = msg.body();

                match body.deserialize::<(u32, HashMap<String, zvariant::OwnedValue>)>() {
                    Ok((_, _results)) => {
                        if let Some(uri_value) = _results.get("uri") {
                            if let Ok(uri) = <&str>::try_from(uri_value) {
                                println!("GTKOVERLAY_RETURNPATH:{}", uri);
                            }
                        }
                    }
                    Err(e) => {
                        eprintln!("Failed to deserialize portal response: {}", e);
                    }
                }
            }
        });
    }

    Ok(())
}

#[tokio::main]
async fn main() {
    let application =
        gtk::Application::new(Some("com.Nexx.GameLibrary.Overlay"), Default::default());

    application.connect_startup(|app| {
        app_startup(app);
    });

    application.run();
}
