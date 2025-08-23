<?php
/**
 * Theme Name: Empire TCG
 * Description: Official theme for Empire Trading Card Game website and store
 * Version: 1.0.0
 * Author: Empire TCG Team
 */

// Theme setup
function empire_theme_setup() {
    // Add theme support
    add_theme_support('post-thumbnails');
    add_theme_support('custom-logo');
    add_theme_support('woocommerce');
    add_theme_support('wc-product-gallery-zoom');
    add_theme_support('wc-product-gallery-lightbox');
    add_theme_support('wc-product-gallery-slider');
    
    // Register navigation menus
    register_nav_menus(array(
        'primary' => __('Primary Menu', 'empire'),
        'footer' => __('Footer Menu', 'empire')
    ));
}
add_action('after_setup_theme', 'empire_theme_setup');

// Enqueue scripts and styles
function empire_scripts() {
    wp_enqueue_style('empire-style', get_stylesheet_uri());
    wp_enqueue_style('empire-custom', get_template_directory_uri() . '/assets/css/empire.css', array(), '1.0.0');
    wp_enqueue_style('font-awesome', 'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css');
    wp_enqueue_style('google-fonts', 'https://fonts.googleapis.com/css2?family=Cinzel:wght@400;600&family=Segoe+UI:wght@400;500;600&display=swap');
    
    wp_enqueue_script('empire-main', get_template_directory_uri() . '/assets/js/empire.js', array('jquery'), '1.0.0', true);
    
    // Localize script for AJAX
    wp_localize_script('empire-main', 'empire_ajax', array(
        'ajax_url' => admin_url('admin-ajax.php'),
        'nonce' => wp_create_nonce('empire_nonce'),
        'game_url' => home_url('/play/'),
        'api_url' => home_url('/game-api/')
    ));
}
add_action('wp_enqueue_scripts', 'empire_scripts');

// Custom post types for Empire content
function empire_custom_post_types() {
    // Game Rules post type
    register_post_type('game_rule', array(
        'labels' => array(
            'name' => 'Game Rules',
            'singular_name' => 'Game Rule'
        ),
        'public' => true,
        'has_archive' => true,
        'supports' => array('title', 'editor', 'thumbnail', 'excerpt'),
        'menu_icon' => 'dashicons-book-alt'
    ));
    
    // Card Database post type
    register_post_type('card_database', array(
        'labels' => array(
            'name' => 'Card Database',
            'singular_name' => 'Card'
        ),
        'public' => true,
        'has_archive' => true,
        'supports' => array('title', 'editor', 'thumbnail', 'custom-fields'),
        'menu_icon' => 'dashicons-id-alt'
    ));
    
    // Tournament post type
    register_post_type('tournament', array(
        'labels' => array(
            'name' => 'Tournaments',
            'singular_name' => 'Tournament'
        ),
        'public' => true,
        'has_archive' => true,
        'supports' => array('title', 'editor', 'thumbnail', 'excerpt'),
        'menu_icon' => 'dashicons-awards'
    ));
}
add_action('init', 'empire_custom_post_types');

// Add Empire TCG colors to customizer
function empire_customize_register($wp_customize) {
    $wp_customize->add_section('empire_colors', array(
        'title' => 'Empire TCG Colors',
        'priority' => 30
    ));
    
    // Primary gold color
    $wp_customize->add_setting('empire_gold_color', array(
        'default' => '#d4af37',
        'sanitize_callback' => 'sanitize_hex_color'
    ));
    
    $wp_customize->add_control(new WP_Customize_Color_Control($wp_customize, 'empire_gold_color', array(
        'label' => 'Empire Gold',
        'section' => 'empire_colors'
    )));
    
    // Dark background color
    $wp_customize->add_setting('empire_dark_color', array(
        'default' => '#1a1a2e',
        'sanitize_callback' => 'sanitize_hex_color'
    ));
    
    $wp_customize->add_control(new WP_Customize_Color_Control($wp_customize, 'empire_dark_color', array(
        'label' => 'Empire Dark',
        'section' => 'empire_colors'
    )));
}
add_action('customize_register', 'empire_customize_register');

// WooCommerce customizations
function empire_woocommerce_setup() {
    // Remove default WooCommerce styles
    add_filter('woocommerce_enqueue_styles', '__return_empty_array');
    
    // Add custom WooCommerce styles
    wp_enqueue_style('empire-woocommerce', get_template_directory_uri() . '/assets/css/woocommerce.css');
}
add_action('wp_enqueue_scripts', 'empire_woocommerce_setup');

// Custom WooCommerce product types for Empire TCG
function empire_product_types($types) {
    $types['empire_card'] = __('Empire Card');
    $types['empire_booster'] = __('Booster Pack');
    $types['empire_deck'] = __('Starter Deck');
    return $types;
}
add_filter('product_type_selector', 'empire_product_types');

// Add game launcher shortcode
function empire_game_launcher_shortcode($atts) {
    $atts = shortcode_atts(array(
        'height' => '800px',
        'width' => '100%',
        'auto_login' => 'true'
    ), $atts);
    
    $iframe_url = home_url('/play/');
    
    // Add current user info for auto-login
    if ($atts['auto_login'] === 'true' && is_user_logged_in()) {
        $current_user = wp_get_current_user();
        $iframe_url .= '?wp_user=' . urlencode($current_user->user_login);
    }
    
    return sprintf(
        '<div class="empire-game-container">
            <iframe src="%s" width="%s" height="%s" frameborder="0" class="empire-game-iframe" allowfullscreen></iframe>
        </div>',
        esc_url($iframe_url),
        esc_attr($atts['width']),
        esc_attr($atts['height'])
    );
}
add_shortcode('empire_game', 'empire_game_launcher_shortcode');

// Add shop integration shortcode
function empire_shop_integration_shortcode($atts) {
    $atts = shortcode_atts(array(
        'category' => '',
        'limit' => '8',
        'columns' => '4'
    ), $atts);
    
    $products = wc_get_products(array(
        'limit' => intval($atts['limit']),
        'category' => array($atts['category']),
        'status' => 'publish'
    ));
    
    if (empty($products)) {
        return '<p>No products found.</p>';
    }
    
    ob_start();
    echo '<div class="empire-products-grid columns-' . esc_attr($atts['columns']) . '">';
    
    foreach ($products as $product) {
        ?>
        <div class="empire-product-item">
            <a href="<?php echo get_permalink($product->get_id()); ?>">
                <?php echo $product->get_image(); ?>
                <h3><?php echo $product->get_name(); ?></h3>
                <div class="price"><?php echo $product->get_price_html(); ?></div>
            </a>
            <button class="empire-add-to-cart" data-product-id="<?php echo $product->get_id(); ?>">
                Add to Cart
            </button>
        </div>
        <?php
    }
    
    echo '</div>';
    return ob_get_clean();
}
add_shortcode('empire_products', 'empire_shop_integration_shortcode');

// AJAX handler for adding to cart
function empire_ajax_add_to_cart() {
    check_ajax_referer('empire_nonce', 'nonce');
    
    $product_id = intval($_POST['product_id']);
    $quantity = intval($_POST['quantity']) ?: 1;
    
    $result = WC()->cart->add_to_cart($product_id, $quantity);
    
    if ($result) {
        wp_send_json_success(array(
            'message' => 'Product added to cart',
            'cart_count' => WC()->cart->get_cart_contents_count()
        ));
    } else {
        wp_send_json_error('Failed to add product to cart');
    }
}
add_action('wp_ajax_empire_add_to_cart', 'empire_ajax_add_to_cart');
add_action('wp_ajax_nopriv_empire_add_to_cart', 'empire_ajax_add_to_cart');

// Add custom CSS variables for theme colors
function empire_custom_css() {
    $gold_color = get_theme_mod('empire_gold_color', '#d4af37');
    $dark_color = get_theme_mod('empire_dark_color', '#1a1a2e');
    ?>
    <style type="text/css">
        :root {
            --empire-gold: <?php echo esc_html($gold_color); ?>;
            --empire-dark: <?php echo esc_html($dark_color); ?>;
            --empire-blue: #16213e;
            --empire-accent: #8b4513;
        }
    </style>
    <?php
}
add_action('wp_head', 'empire_custom_css');
?>