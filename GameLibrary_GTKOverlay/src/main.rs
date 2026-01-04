use std::collections::HashMap;

use gtk::{gdk_pixbuf::Pixbuf, prelude::*};
use zbus::zvariant::{OwnedValue, OwnedObjectPath};
use zbus::blocking::{Connection, Proxy};

fn app_startup(application: &gtk::Application) {
    let window = gtk::ApplicationWindow::new(application);
    window.set_size_request(100, 40);

    let is_gnome: bool = get_is_gnome();

    if is_gnome {
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
        window.hide();
        
        let window_clone = window.clone();
        glib::idle_add_local_once(move || {
            screenshot_process(&window_clone, is_gnome);    
            window_clone.close();
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

fn screenshot_process(_window: &gtk::ApplicationWindow, is_gnome: bool) {
    let connection = Connection::session().unwrap();

    let proxy = Proxy::new(
        &connection,
        "org.freedesktop.portal.Desktop",
        "/org/freedesktop/portal/desktop",
        "org.freedesktop.portal.Screenshot",
    ).unwrap();

    let mut options: HashMap<&str, OwnedValue> = HashMap::new();

    options.insert("modal", OwnedValue::from(true));
    options.insert("interactive", OwnedValue::from(true));
    
    let (_handle_path,): (OwnedObjectPath,) =
        proxy.call("Screenshot", &("", options)).unwrap();

    if is_gnome {
        std::thread::sleep(std::time::Duration::from_secs(2));

        if let Some(pixbuf) = get_screenshot_from_clipboard() {
            let path = "/tmp/screenshot.png";
            pixbuf.savev(path, "png", &[]).unwrap();
            println!("GTKOVERLAY_RETURNPATH: {}", path);
        } else {
            eprintln!("No image in clipboard");
        }
    } else {
        let handle_path = _handle_path.to_string();
        
        let request_proxy = Proxy::new(
            &connection,
            "org.freedesktop.portal.Desktop",
            handle_path,
            "org.freedesktop.portal.Request",
        ).unwrap();
        
        let mut signal_stream = request_proxy.receive_signal("Response").unwrap();
        
        if let Some(msg) = signal_stream.next() {
            let body = msg.body();
            
            match body.deserialize::<(u32, HashMap<String, OwnedValue>)>() {
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
    }
}

fn main() {
    let application =
        gtk::Application::new(Some("com.Nexx.GameLibrary.Overlay"), Default::default());

    application.connect_startup(|app| {
        app_startup(app);
    });

    application.run();
}
