<!DOCTYPE html>
<html <?php language_attributes(); ?>>
<head>
    <meta charset="<?php bloginfo('charset'); ?>">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title><?php wp_title('|', true, 'right'); ?><?php bloginfo('name'); ?></title>
    <?php wp_head(); ?>
</head>

<body <?php body_class(); ?>>
    <?php wp_body_open(); ?>
    
    <div id="page" class="site">
        <header id="masthead" class="site-header">
            <div class="header-container">
                <div class="site-branding">
                    <?php if (has_custom_logo()) : ?>
                        <?php the_custom_logo(); ?>
                    <?php else : ?>
                        <h1 class="site-title">
                            <a href="<?php echo esc_url(home_url('/')); ?>">
                                ?? <?php bloginfo('name'); ?> ??
                            </a>
                        </h1>
                    <?php endif; ?>
                </div>

                <nav id="site-navigation" class="main-navigation">
                    <?php
                    wp_nav_menu(array(
                        'theme_location' => 'primary',
                        'menu_id' => 'primary-menu',
                        'container' => false,
                        'menu_class' => 'nav-menu'
                    ));
                    ?>
                    
                    <!-- WooCommerce Cart -->
                    <?php if (class_exists('WooCommerce')) : ?>
                        <div class="header-cart">
                            <a href="<?php echo wc_get_cart_url(); ?>" class="cart-link">
                                <i class="fas fa-shopping-cart"></i>
                                <span class="cart-count"><?php echo WC()->cart->get_cart_contents_count(); ?></span>
                            </a>
                        </div>
                    <?php endif; ?>

                    <!-- Game Login Status -->
                    <div class="game-status">
                        <?php if (is_user_logged_in()) : ?>
                            <a href="/play/" class="play-button">
                                <i class="fas fa-gamepad"></i> Play Game
                            </a>
                            <a href="<?php echo wp_logout_url(home_url()); ?>" class="logout-link">
                                <i class="fas fa-sign-out-alt"></i>
                            </a>
                        <?php else : ?>
                            <a href="<?php echo wp_login_url(); ?>" class="login-button">
                                <i class="fas fa-sign-in-alt"></i> Login
                            </a>
                        <?php endif; ?>
                    </div>
                </nav>

                <!-- Mobile menu toggle -->
                <button class="menu-toggle" aria-controls="primary-menu" aria-expanded="false">
                    <i class="fas fa-bars"></i>
                </button>
            </div>
        </header>

        <div id="content" class="site-content">
            <?php if (is_front_page()) : ?>
                <!-- Hero Section for Homepage -->
                <section class="hero-section">
                    <div class="hero-content">
                        <h1 class="hero-title">Welcome to Empire TCG</h1>
                        <p class="hero-subtitle">Build your empire, command your armies, conquer your enemies</p>
                        <div class="hero-actions">
                            <a href="/play/" class="btn btn-primary btn-large">
                                <i class="fas fa-play"></i> Play Now
                            </a>
                            <a href="/shop/" class="btn btn-secondary btn-large">
                                <i class="fas fa-shopping-bag"></i> Shop Cards
                            </a>
                            <a href="/rules/" class="btn btn-outline btn-large">
                                <i class="fas fa-book"></i> Learn Rules
                            </a>
                        </div>
                    </div>
                    <div class="hero-background">
                        <div class="hero-particles"></div>
                    </div>
                </section>

                <!-- Quick Stats -->
                <section class="stats-section">
                    <div class="container">
                        <div class="stats-grid">
                            <div class="stat-item">
                                <div class="stat-number"><?php echo get_users(array('count_total' => true)); ?></div>
                                <div class="stat-label">Players</div>
                            </div>
                            <div class="stat-item">
                                <div class="stat-number"><?php echo wp_count_posts('card_database')->publish; ?></div>
                                <div class="stat-label">Cards</div>
                            </div>
                            <div class="stat-item">
                                <div class="stat-number"><?php echo wp_count_posts('tournament')->publish; ?></div>
                                <div class="stat-label">Tournaments</div>
                            </div>
                            <div class="stat-item">
                                <div class="stat-number">?</div>
                                <div class="stat-label">Strategies</div>
                            </div>
                        </div>
                    </div>
                </section>
            <?php endif; ?>

            <main id="main" class="site-main">
                <?php if (have_posts()) : ?>
                    <div class="content-container">
                        <?php while (have_posts()) : the_post(); ?>
                            <article id="post-<?php the_ID(); ?>" <?php post_class(); ?>>
                                <?php if (is_singular() && !is_front_page()) : ?>
                                    <header class="entry-header">
                                        <h1 class="entry-title"><?php the_title(); ?></h1>
                                        <?php if (get_post_type() === 'post') : ?>
                                            <div class="entry-meta">
                                                <span class="posted-on">
                                                    <i class="fas fa-calendar"></i>
                                                    <?php echo get_the_date(); ?>
                                                </span>
                                                <span class="byline">
                                                    <i class="fas fa-user"></i>
                                                    <?php the_author(); ?>
                                                </span>
                                            </div>
                                        <?php endif; ?>
                                    </header>
                                <?php endif; ?>

                                <div class="entry-content">
                                    <?php
                                    if (is_singular()) {
                                        the_content();
                                    } else {
                                        the_excerpt();
                                    }
                                    ?>
                                </div>

                                <?php if (!is_singular()) : ?>
                                    <footer class="entry-footer">
                                        <a href="<?php the_permalink(); ?>" class="read-more">
                                            Read More <i class="fas fa-arrow-right"></i>
                                        </a>
                                    </footer>
                                <?php endif; ?>
                            </article>
                        <?php endwhile; ?>

                        <?php
                        // Pagination
                        the_posts_navigation(array(
                            'prev_text' => '<i class="fas fa-chevron-left"></i> Previous',
                            'next_text' => 'Next <i class="fas fa-chevron-right"></i>'
                        ));
                        ?>
                    </div>
                <?php else : ?>
                    <div class="no-content">
                        <h2>Nothing Found</h2>
                        <p>It looks like nothing was found at this location. Maybe try a search?</p>
                        <?php get_search_form(); ?>
                    </div>
                <?php endif; ?>
            </main>
        </div>

        <footer id="colophon" class="site-footer">
            <div class="footer-container">
                <div class="footer-content">
                    <div class="footer-section">
                        <h3>Empire TCG</h3>
                        <p>The ultimate strategic trading card game experience.</p>
                        <div class="social-links">
                            <a href="#" class="social-link"><i class="fab fa-twitter"></i></a>
                            <a href="#" class="social-link"><i class="fab fa-discord"></i></a>
                            <a href="#" class="social-link"><i class="fab fa-youtube"></i></a>
                        </div>
                    </div>
                    
                    <div class="footer-section">
                        <h3>Quick Links</h3>
                        <?php
                        wp_nav_menu(array(
                            'theme_location' => 'footer',
                            'container' => false,
                            'menu_class' => 'footer-menu'
                        ));
                        ?>
                    </div>
                    
                    <div class="footer-section">
                        <h3>Game Resources</h3>
                        <ul class="footer-links">
                            <li><a href="/rules/">Game Rules</a></li>
                            <li><a href="/cards/">Card Database</a></li>
                            <li><a href="/tournaments/">Tournaments</a></li>
                            <li><a href="/downloads/">Downloads</a></li>
                        </ul>
                    </div>
                    
                    <div class="footer-section">
                        <h3>Support</h3>
                        <ul class="footer-links">
                            <li><a href="/contact/">Contact Us</a></li>
                            <li><a href="/faq/">FAQ</a></li>
                            <li><a href="/support/">Game Support</a></li>
                            <li><a href="/community/">Community</a></li>
                        </ul>
                    </div>
                </div>
                
                <div class="footer-bottom">
                    <div class="copyright">
                        <p>&copy; <?php echo date('Y'); ?> Empire TCG. All rights reserved.</p>
                    </div>
                    <div class="powered-by">
                        <p>Powered by <a href="https://wordpress.org">WordPress</a> & <a href="#">.NET</a></p>
                    </div>
                </div>
            </div>
        </footer>
    </div>

    <?php wp_footer(); ?>
</body>
</html>