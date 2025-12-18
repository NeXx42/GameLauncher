use gio::prelude::*;
use gtk::prelude::*;
use std::collections::HashMap;

use futures_util::stream::StreamExt;
use zbus::Connection;

fn app_startup(application: &gtk::Application) {
    let window = gtk::ApplicationWindow::new(application);
    window.set_size_request(100, 40);

    if is_gnome() {
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
        // Use glib::MainContext to avoid Tokio runtime issues
        glib::MainContext::default().spawn_local(async move {
            if let Err(e) = screenshot_process(&window).await {
                eprintln!("Screenshot failed: {}", e);
            }
        });
    });

    vbox.add(&close_button);
    vbox.add(&screenshot_button);

    window.set_child(Some(&vbox));
    window.show_all();
    window.present();
}

fn is_gnome() -> bool {
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

async fn screenshot_process(window: &gtk::ApplicationWindow) -> zbus::Result<()> {
    let connection = Connection::session().await?;

    let proxy = zbus::Proxy::new(
        &connection,
        "org.freedesktop.portal.Desktop",
        "/org/freedesktop/portal/desktop",
        "org.freedesktop.portal.Screenshot",
    )
    .await?;

    let parent_handle = window
        .window() // GDK window
        .map(|w| <gtk::gdk::Window as AsRef<gtk::gdk::Window>>::as_ref(&w).to_string()) // handle string
        .unwrap_or_default();
    let parent_handle = "";
    let mut options: HashMap<&str, zvariant::OwnedValue> = HashMap::new();

    options.insert("modal", zvariant::OwnedValue::from(true));
    options.insert("interactive", zvariant::OwnedValue::from(true));

    let (_handle_path,): (zvariant::OwnedObjectPath,) =
        proxy.call("Screenshot", &(parent_handle, options)).await?;

    let handle_path = _handle_path.to_string();
    let request_proxy = zbus::Proxy::new(
        &connection,
        "org.freedesktop.portal.Desktop",
        handle_path,
        "org.freedesktop.portal.Request",
    )
    .await?;

    // Listen for the response
    //let mut stream = request_proxy.receive_signal("Response").await?;
    //while let Some(msg) = stream.next().await {
    //    let body = msg.body();
    //    let (response_code, results): (u32, HashMap<String, zvariant::OwnedValue>) =
    //        body.deserialize()?;
    //
    //    if response_code == 0 {
    //        if let Some(uri_value) = results.get("uri") {
    //            // Try to extract as a string reference
    //            if let Ok(uri) = <&str>::try_from(uri_value) {
    //                println!("GTKOVERLAY_RETURNPATH:{}", uri);
    //            }
    //        }
    //    }
    //    break;
    //}

    let request = request_proxy.clone();
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

            // Fix: deserialize returns a Result wrapping the tuple
            match body.deserialize::<(u32, HashMap<String, zvariant::OwnedValue>)>() {
                Ok((code, _results)) => {
                    if let Some(uri_value) = _results.get("uri") {
                        // Try to extract as a string reference
                        if let Ok(uri) = <&str>::try_from(uri_value) {
                            println!("GTKOVERLAY_RETURNPATH:{}", uri);
                        }
                    }

                    println!("Portal returned code: {}", code);
                    println!("Full results from portal:");
                    for (key, value) in &_results {
                        println!("  {}: {:?}", key, value);
                    }
                }
                Err(e) => {
                    eprintln!("Failed to deserialize portal response: {}", e);
                }
            }
        }
    });

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
