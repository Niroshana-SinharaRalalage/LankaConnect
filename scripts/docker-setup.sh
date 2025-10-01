#!/bin/bash

# LankaConnect Docker Development Environment Setup Script
# Bash script to set up and manage Docker development environment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default values
ACTION="up"
SERVICE=""
BUILD=false
DETACHED=true

# Help function
show_help() {
    echo -e "${CYAN}LankaConnect Docker Environment Manager${NC}"
    echo -e "${CYAN}=======================================${NC}"
    echo ""
    echo "Usage: $0 [ACTION] [OPTIONS]"
    echo ""
    echo "Actions:"
    echo "  up       Start the development environment (default)"
    echo "  down     Stop the development environment"
    echo "  restart  Restart services"
    echo "  logs     Show service logs"
    echo "  status   Show service status"
    echo "  clean    Clean up containers, volumes, and networks"
    echo ""
    echo "Options:"
    echo "  -s, --service SERVICE    Target specific service"
    echo "  -b, --build             Force rebuild of images"
    echo "  -f, --foreground        Run in foreground (don't detach)"
    echo "  -h, --help              Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 up                   Start all services"
    echo "  $0 up -s postgres       Start only PostgreSQL"
    echo "  $0 logs -s redis        Show Redis logs"
    echo "  $0 restart -s mailhog   Restart MailHog service"
}

# Check if Docker is running
check_docker() {
    if ! docker info >/dev/null 2>&1; then
        echo -e "${RED}Error: Docker is not running. Please start Docker and try again.${NC}"
        exit 1
    fi
}

# Check if docker-compose.yml exists
check_compose_file() {
    if [ ! -f "docker-compose.yml" ]; then
        echo -e "${RED}Error: docker-compose.yml not found. Please run this script from the project root directory.${NC}"
        exit 1
    fi
}

# Show service status and URLs
show_status() {
    echo -e "\n${YELLOW}Service Status:${NC}"
    docker-compose ps
    
    echo -e "\n${YELLOW}Service URLs:${NC}"
    echo -e "${GREEN}PostgreSQL: localhost:5432${NC}"
    echo -e "${GREEN}Redis: localhost:6379${NC}"
    echo -e "${GREEN}MailHog UI: http://localhost:8025${NC}"
    echo -e "${GREEN}Azurite Blob: http://localhost:10000${NC}"
    echo -e "${GREEN}Seq Logs: http://localhost:8080${NC}"
    echo -e "${GREEN}pgAdmin: http://localhost:8081${NC}"
    echo -e "${GREEN}Redis Commander: http://localhost:8082${NC}"
}

# Parse command line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            up|down|restart|logs|status|clean)
                ACTION="$1"
                shift
                ;;
            -s|--service)
                SERVICE="$2"
                shift 2
                ;;
            -b|--build)
                BUILD=true
                shift
                ;;
            -f|--foreground)
                DETACHED=false
                shift
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            *)
                echo -e "${RED}Unknown option: $1${NC}"
                show_help
                exit 1
                ;;
        esac
    done
}

# Main execution
main() {
    parse_args "$@"
    
    echo -e "${CYAN}LankaConnect Docker Environment Manager${NC}"
    echo -e "${CYAN}=======================================${NC}"
    
    check_docker
    check_compose_file
    
    case $ACTION in
        "up")
            echo -e "${GREEN}Starting LankaConnect development environment...${NC}"
            
            compose_args="up"
            
            if [ "$DETACHED" = true ]; then
                compose_args="$compose_args -d"
            fi
            
            if [ "$BUILD" = true ]; then
                compose_args="$compose_args --build"
            fi
            
            if [ -n "$SERVICE" ]; then
                compose_args="$compose_args $SERVICE"
            fi
            
            docker-compose $compose_args
            
            if [ $? -eq 0 ]; then
                sleep 5
                show_status
                echo -e "\n${GREEN}Environment setup complete!${NC}"
                echo -e "${YELLOW}Copy .env.docker to .env.local and modify as needed.${NC}"
            fi
            ;;
        
        "down")
            echo -e "${YELLOW}Stopping LankaConnect development environment...${NC}"
            docker-compose down
            ;;
        
        "restart")
            echo -e "${YELLOW}Restarting LankaConnect development environment...${NC}"
            docker-compose restart $SERVICE
            sleep 3
            show_status
            ;;
        
        "logs")
            if [ -n "$SERVICE" ]; then
                docker-compose logs -f $SERVICE
            else
                docker-compose logs -f
            fi
            ;;
        
        "status")
            show_status
            ;;
        
        "clean")
            echo -e "${RED}Cleaning up Docker environment...${NC}"
            echo -e "${RED}This will remove containers, volumes, and networks.${NC}"
            
            read -p "Are you sure? (y/N): " -n 1 -r
            echo
            if [[ $REPLY =~ ^[Yy]$ ]]; then
                docker-compose down -v --remove-orphans
                docker system prune -f
                echo -e "${GREEN}Cleanup complete.${NC}"
            else
                echo -e "${YELLOW}Cleanup cancelled.${NC}"
            fi
            ;;
    esac
    
    echo -e "\n${CYAN}Script execution completed.${NC}"
}

# Run main function with all arguments
main "$@"