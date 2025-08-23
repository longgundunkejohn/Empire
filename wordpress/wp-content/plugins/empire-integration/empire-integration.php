<?php
/**
 * Plugin Name: Empire TCG Integration
 * Plugin URI: https://empirecardgame.com
 * Description: Integrates the Empire TCG Blazor game with WordPress, provides Stripe payments, and manages game accounts
 * Version: 1.0.0
 * Author: Empire TCG Team
 * License: GPL v2 or later
 */

// Prevent direct access
if (!defined('ABSPATH')) {
    exit;
}

// Plugin constants
define('EMPIRE_PLUGIN_VERSION', '1.0.0');
define('EMPIRE_PLUGIN_PATH', plugin_dir_path(__FILE__));
define('EMPIRE_PLUGIN_URL', plugin_dir_url(__FILE__));

// Main plugin class
class EmpireTCGIntegration {
    
    public function __construct() {
        add_action('init', array($this, 'init'));
        add_action('rest_api_init', array($this, 'register_api_routes'));
        add_action('wp_enqueue_scripts', array($this, 'enqueue_scripts'));
        add_action('admin_menu', array($this, 'add_admin_menu'));
        
        // WooCommerce hooks
        add_action('woocommerce_init', array($this, 'woocommerce_integration'));
        
        // User registration hooks
        add_action('user_register', array($this, 'sync_new_user_with_game'));
        add_action('wp_login', array($this, 'update_game_login_status'), 10, 2);
    }
    
    public function init() {
        // Register custom post types
        $this->register_post_types();
        
        // Add shortcodes
        add_shortcode('empire_game', array($this, 'game_shortcode'));
        add_shortcode('empire_shop', array($this, 'shop_shortcode'));
        add_shortcode('empire_leaderboard', array($this, 'leaderboard_shortcode'));
    }
    
    public function register_post_types() {
        // Game Statistics post type
        register_post_type('game_stats', array(
            'labels' => array(
                'name' => 'Game Statistics',
                'singular_name' => 'Game Stat'
            ),
            'public' => false,
            'show_ui' => true,
            'show_in_menu' => 'empire-tcg',
            'supports' => array('title', 'custom-fields'),
            'capability_type' => 'post'
        ));
        
        // Player Profiles post type
        register_post_type('player_profile', array(
            'labels' => array(
                'name' => 'Player Profiles',
                'singular_name' => 'Player Profile'
            ),
            'public' => true,
            'show_ui' => true,
            'show_in_menu' => 'empire-tcg',
            'supports' => array('title', 'editor', 'thumbnail', 'custom-fields'),
            'has_archive' => true,
            'rewrite' => array('slug' => 'players')
        ));
    }
    
    public function register_api_routes() {
        // Game integration endpoints
        register_rest_route('empire/v1', '/sync-account', array(
            'methods' => 'POST',
            'callback' => array($this, 'sync_account_endpoint'),
            'permission_callback' => array($this, 'check_user_permissions')
        ));
        
        register_rest_route('empire/v1', '/checkout', array(
            'methods' => 'POST',
            'callback' => array($this, 'create_stripe_checkout'),
            'permission_callback' => array($this, 'check_user_permissions')
        ));
        
        register_rest_route('empire/v1', '/game-stats', array(
            'methods' => array('GET', 'POST'),
            'callback' => array($this, 'handle_game_stats'),
            'permission_callback' => array($this, 'check_user_permissions')
        ));
        
        register_rest_route('empire/v1', '/leaderboard', array(
            'methods' => 'GET',
            'callback' => array($this, 'get_leaderboard'),
            'permission_callback' => '__return_true'
        ));
    }
    
    public function enqueue_scripts() {
        wp_enqueue_script(
            'empire-integration',
            EMPIRE_PLUGIN_URL . 'assets/empire-integration.js',
            array('jquery'),
            EMPIRE_PLUGIN_VERSION,
            true
        );
        
        wp_localize_script('empire-integration', 'empire_vars', array(
            'ajax_url' => admin_url('admin-ajax.php'),
            'rest_url' => rest_url('empire/v1/'),
            'nonce' => wp_create_nonce('empire_nonce'),
            'game_url' => home_url('/play/'),
            'current_user_id' => get_current_user_id(),
            'stripe_public_key' => get_option('empire_stripe_public_key', '')
        ));
        
        wp_enqueue_style(
            'empire-integration',
            EMPIRE_PLUGIN_URL . 'assets/empire-integration.css',
            array(),
            EMPIRE_PLUGIN_VERSION
        );
    }
    
    // Admin menu
    public function add_admin_menu() {
        add_menu_page(
            'Empire TCG',
            'Empire TCG',
            'manage_options',
            'empire-tcg',
            array($this, 'admin_dashboard'),
            'dashicons-games',
            30
        );
        
        add_submenu_page(
            'empire-tcg',
            'Settings',
            'Settings',
            'manage_options',
            'empire-settings',
            array($this, 'admin_settings')
        );
        
        add_submenu_page(
            'empire-tcg',
            'Game Statistics',
            'Statistics',
            'manage_options',
            'empire-stats',
            array($this, 'admin_stats')
        );
    }
    
    // Shortcode: Game embed
    public function game_shortcode($atts) {
        $atts = shortcode_atts(array(
            'height' => '800px',
            'width' => '100%',
            'auto_login' => 'true',
            'redirect_on_exit' => home_url()
        ), $atts);
        
        $game_url = home_url('/play/');
        
        // Add WordPress user info for cross-platform login
        if ($atts['auto_login'] === 'true' && is_user_logged_in()) {
            $current_user = wp_get_current_user();
            $game_url .= '?wp_user=' . urlencode($current_user->user_login) . 
                        '&wp_token=' . wp_create_nonce('empire_game_' . $current_user->ID);
        }
        
        ob_start();
        ?>
        <div class="empire-game-embed" data-redirect="<?php echo esc_attr($atts['redirect_on_exit']); ?>">
            <div class="game-loading">
                <div class="loading-spinner"></div>
                <p>Loading Empire TCG...</p>
            </div>
            <iframe 
                src="<?php echo esc_url($game_url); ?>" 
                width="<?php echo esc_attr($atts['width']); ?>" 
                height="<?php echo esc_attr($atts['height']); ?>" 
                frameborder="0" 
                class="empire-game-frame"
                allowfullscreen>
            </iframe>
        </div>
        <?php
        return ob_get_clean();
    }
    
    // Shortcode: Shop integration
    public function shop_shortcode($atts) {
        $atts = shortcode_atts(array(
            'category' => '',
            'limit' => 8,
            'columns' => 4,
            'show_cart' => 'true'
        ), $atts);
        
        if (!class_exists('WooCommerce')) {
            return '<p>WooCommerce is required for the shop functionality.</p>';
        }
        
        $args = array(
            'limit' => intval($atts['limit']),
            'status' => 'publish'
        );
        
        if (!empty($atts['category'])) {
            $args['category'] = array($atts['category']);
        }
        
        $products = wc_get_products($args);
        
        ob_start();
        ?>
        <div class="empire-shop-integration" data-columns="<?php echo esc_attr($atts['columns']); ?>">
            <?php if ($atts['show_cart'] === 'true'): ?>
                <div class="mini-cart">
                    <a href="<?php echo wc_get_cart_url(); ?>" class="cart-summary">
                        <i class="fas fa-shopping-cart"></i>
                        <span class="cart-count"><?php echo WC()->cart->get_cart_contents_count(); ?></span>
                        <span class="cart-total"><?php echo WC()->cart->get_cart_total(); ?></span>
                    </a>
                </div>
            <?php endif; ?>
            
            <div class="products-grid columns-<?php echo esc_attr($atts['columns']); ?>">
                <?php foreach ($products as $product): ?>
                    <div class="product-card" data-product-id="<?php echo $product->get_id(); ?>">
                        <div class="product-image">
                            <?php echo $product->get_image(); ?>
                        </div>
                        <div class="product-info">
                            <h3 class="product-title"><?php echo $product->get_name(); ?></h3>
                            <div class="product-price"><?php echo $product->get_price_html(); ?></div>
                            <div class="product-actions">
                                <button class="add-to-cart-btn" data-product-id="<?php echo $product->get_id(); ?>">
                                    Add to Cart
                                </button>
                                <a href="<?php echo get_permalink($product->get_id()); ?>" class="view-product-btn">
                                    View Details
                                </a>
                            </div>
                        </div>
                    </div>
                <?php endforeach; ?>
            </div>
        </div>
        <?php
        return ob_get_clean();
    }
    
    // Shortcode: Leaderboard
    public function leaderboard_shortcode($atts) {
        $atts = shortcode_atts(array(
            'limit' => 10,
            'metric' => 'wins',
            'period' => 'all_time'
        ), $atts);
        
        $leaderboard_data = $this->get_leaderboard_data($atts);
        
        ob_start();
        ?>
        <div class="empire-leaderboard">
            <h3>Top Players - <?php echo ucfirst(str_replace('_', ' ', $atts['metric'])); ?></h3>
            <div class="leaderboard-filters">
                <select class="period-filter" data-current="<?php echo esc_attr($atts['period']); ?>">
                    <option value="all_time">All Time</option>
                    <option value="monthly">This Month</option>
                    <option value="weekly">This Week</option>
                </select>
            </div>
            <div class="leaderboard-list">
                <?php foreach ($leaderboard_data as $index => $player): ?>
                    <div class="leaderboard-item rank-<?php echo $index + 1; ?>">
                        <div class="rank">#<?php echo $index + 1; ?></div>
                        <div class="player-info">
                            <div class="player-name"><?php echo esc_html($player['name']); ?></div>
                            <div class="player-stats"><?php echo esc_html($player['value']); ?> <?php echo esc_html($atts['metric']); ?></div>
                        </div>
                    </div>
                <?php endforeach; ?>
            </div>
        </div>
        <?php
        return ob_get_clean();
    }
    
    // API Endpoints
    public function sync_account_endpoint($request) {
        $params = $request->get_params();
        $user_id = get_current_user_id();
        
        if (!$user_id) {
            return new WP_Error('not_authenticated', 'User must be logged in', array('status' => 401));
        }
        
        // Update user meta with game account info
        update_user_meta($user_id, 'empire_username', sanitize_text_field($params['empire_username']));
        update_user_meta($user_id, 'empire_user_id', intval($params['empire_user_id']));
        update_user_meta($user_id, 'empire_sync_date', current_time('mysql'));
        
        return rest_ensure_response(array(
            'success' => true,
            'message' => 'Account synced successfully'
        ));
    }
    
    public function create_stripe_checkout($request) {
        if (!class_exists('WooCommerce')) {
            return new WP_Error('woocommerce_required', 'WooCommerce is required', array('status' => 400));
        }
        
        $params = $request->get_params();
        $user_id = get_current_user_id();
        
        if (!$user_id) {
            return new WP_Error('not_authenticated', 'User must be logged in', array('status' => 401));
        }
        
        try {
            // Initialize Stripe
            $stripe_secret = get_option('empire_stripe_secret_key');
            if (!$stripe_secret) {
                throw new Exception('Stripe not configured');
            }
            
            \Stripe\Stripe::setApiKey($stripe_secret);
            
            // Create checkout session
            $session = \Stripe\Checkout\Session::create([
                'payment_method_types' => ['card'],
                'line_items' => $this->format_stripe_line_items($params['items']),
                'mode' => 'payment',
                'success_url' => $params['success_url'] . '?session_id={CHECKOUT_SESSION_ID}',
                'cancel_url' => $params['cancel_url'],
                'customer_email' => $params['customer_email'],
                'metadata' => [
                    'wp_user_id' => $user_id,
                    'empire_integration' => 'true'
                ]
            ]);
            
            return rest_ensure_response(array(
                'id' => $session->id,
                'url' => $session->url
            ));
            
        } catch (Exception $e) {
            return new WP_Error('stripe_error', $e->getMessage(), array('status' => 400));
        }
    }
    
    public function handle_game_stats($request) {
        $method = $request->get_method();
        $user_id = get_current_user_id();
        
        if (!$user_id) {
            return new WP_Error('not_authenticated', 'User must be logged in', array('status' => 401));
        }
        
        if ($method === 'GET') {
            // Return user's game statistics
            $stats = get_user_meta($user_id, 'empire_game_stats', true);
            return rest_ensure_response($stats ?: array());
        } else {
            // Update user's game statistics
            $params = $request->get_params();
            $current_stats = get_user_meta($user_id, 'empire_game_stats', true) ?: array();
            
            // Merge new stats with existing ones
            $updated_stats = array_merge($current_stats, $params);
            update_user_meta($user_id, 'empire_game_stats', $updated_stats);
            
            return rest_ensure_response(array('success' => true));
        }
    }
    
    public function get_leaderboard($request) {
        $params = $request->get_params();
        $metric = sanitize_text_field($params['metric'] ?? 'wins');
        $limit = intval($params['limit'] ?? 10);
        
        return rest_ensure_response($this->get_leaderboard_data(array(
            'metric' => $metric,
            'limit' => $limit,
            'period' => $params['period'] ?? 'all_time'
        )));
    }
    
    // Helper methods
    private function get_leaderboard_data($args) {
        // This would query the database for actual game statistics
        // For now, return mock data
        return array(
            array('name' => 'EmpireVet', 'value' => 127),
            array('name' => 'Strategist', 'value' => 98),
            array('name' => 'CardMaster', 'value' => 76),
            array('name' => 'TacticalGenius', 'value' => 65),
            array('name' => 'VictorySeeker', 'value' => 54)
        );
    }
    
    private function format_stripe_line_items($items) {
        $line_items = array();
        foreach ($items as $item) {
            $line_items[] = array(
                'price_data' => array(
                    'currency' => 'usd',
                    'product_data' => array(
                        'name' => $item['name']
                    ),
                    'unit_amount' => intval($item['price'] * 100) // Convert to cents
                ),
                'quantity' => $item['quantity']
            );
        }
        return $line_items;
    }
    
    public function check_user_permissions($request) {
        return is_user_logged_in();
    }
    
    // WooCommerce integration
    public function woocommerce_integration() {
        // Add Empire TCG specific product types
        add_filter('product_type_selector', array($this, 'add_empire_product_types'));
        
        // Custom checkout processing
        add_action('woocommerce_checkout_order_processed', array($this, 'process_empire_order'));
    }
    
    public function add_empire_product_types($types) {
        $types['empire_card'] = __('Empire Card');
        $types['empire_booster'] = __('Booster Pack');
        $types['empire_deck'] = __('Starter Deck');
        return $types;
    }
    
    public function process_empire_order($order_id) {
        // Handle Empire-specific order processing
        // This could include digital card delivery, account credits, etc.
        $order = wc_get_order($order_id);
        $user_id = $order->get_user_id();
        
        if ($user_id) {
            // Log the purchase for game integration
            $this->log_purchase_for_game($user_id, $order);
        }
    }
    
    private function log_purchase_for_game($user_id, $order) {
        // This would integrate with the game server to deliver digital items
        // For now, just log the purchase
        add_user_meta($user_id, 'empire_purchase_log', array(
            'order_id' => $order->get_id(),
            'total' => $order->get_total(),
            'items' => $order->get_items(),
            'date' => current_time('mysql')
        ));
    }
    
    // User synchronization
    public function sync_new_user_with_game($user_id) {
        // When a new WordPress user registers, create a corresponding game account
        $user = get_user_by('id', $user_id);
        
        // This would make an API call to your game server
        // wp_remote_post(home_url('/game-api/sync-user'), array(...));
    }
    
    public function update_game_login_status($user_login, $user) {
        // Update the user's last login time for game integration
        update_user_meta($user->ID, 'empire_last_wp_login', current_time('mysql'));
    }
    
    // Admin pages
    public function admin_dashboard() {
        ?>
        <div class="wrap">
            <h1>Empire TCG Dashboard</h1>
            <div class="empire-admin-dashboard">
                <div class="dashboard-stats">
                    <div class="stat-box">
                        <h3>Total Players</h3>
                        <p class="stat-number"><?php echo count_users()['total_users']; ?></p>
                    </div>
                    <div class="stat-box">
                        <h3>Active Games</h3>
                        <p class="stat-number">--</p>
                    </div>
                    <div class="stat-box">
                        <h3>Store Revenue</h3>
                        <p class="stat-number">$--</p>
                    </div>
                </div>
                
                <div class="quick-actions">
                    <h2>Quick Actions</h2>
                    <a href="<?php echo admin_url('admin.php?page=empire-settings'); ?>" class="button button-primary">Settings</a>
                    <a href="<?php echo admin_url('admin.php?page=empire-stats'); ?>" class="button">View Statistics</a>
                    <a href="<?php echo home_url('/play/'); ?>" class="button" target="_blank">Launch Game</a>
                </div>
            </div>
        </div>
        <?php
    }
    
    public function admin_settings() {
        if (isset($_POST['submit'])) {
            update_option('empire_stripe_public_key', sanitize_text_field($_POST['stripe_public_key']));
            update_option('empire_stripe_secret_key', sanitize_text_field($_POST['stripe_secret_key']));
            update_option('empire_game_server_url', esc_url_raw($_POST['game_server_url']));
            echo '<div class="notice notice-success"><p>Settings saved!</p></div>';
        }
        
        $stripe_public = get_option('empire_stripe_public_key', '');
        $stripe_secret = get_option('empire_stripe_secret_key', '');
        $game_server_url = get_option('empire_game_server_url', home_url('/play/'));
        ?>
        <div class="wrap">
            <h1>Empire TCG Settings</h1>
            <form method="post" action="">
                <table class="form-table">
                    <tr>
                        <th scope="row">Stripe Public Key</th>
                        <td><input type="text" name="stripe_public_key" value="<?php echo esc_attr($stripe_public); ?>" class="regular-text" /></td>
                    </tr>
                    <tr>
                        <th scope="row">Stripe Secret Key</th>
                        <td><input type="password" name="stripe_secret_key" value="<?php echo esc_attr($stripe_secret); ?>" class="regular-text" /></td>
                    </tr>
                    <tr>
                        <th scope="row">Game Server URL</th>
                        <td><input type="url" name="game_server_url" value="<?php echo esc_attr($game_server_url); ?>" class="regular-text" /></td>
                    </tr>
                </table>
                <?php submit_button(); ?>
            </form>
        </div>
        <?php
    }
    
    public function admin_stats() {
        ?>
        <div class="wrap">
            <h1>Empire TCG Statistics</h1>
            <div class="empire-stats-dashboard">
                <p>Game statistics and analytics will be displayed here.</p>
                <!-- This would show real game statistics -->
            </div>
        </div>
        <?php
    }
}

// Initialize the plugin
new EmpireTCGIntegration();

// Activation hook
register_activation_hook(__FILE__, 'empire_plugin_activate');
function empire_plugin_activate() {
    // Create necessary database tables or options
    flush_rewrite_rules();
}

// Deactivation hook
register_deactivation_hook(__FILE__, 'empire_plugin_deactivate');
function empire_plugin_deactivate() {
    flush_rewrite_rules();
}
?>