import { Bell, Search, Menu, X, Globe } from 'lucide-react';
import { Button } from './ui/button';
import { Avatar, AvatarFallback, AvatarImage } from './ui/avatar';
import { Input } from './ui/input';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from './ui/dropdown-menu';
import { Sheet, SheetContent, SheetTrigger } from './ui/sheet';
import { Badge } from './ui/badge';
import logoImage from 'figma:asset/ddff46fa3f682c184b1a51df378cf72d9bdcff47.png';

export function Header() {
  const navItems = [
    { label: 'Events', href: '#events' },
    { label: 'Marketplace', href: '#marketplace' },
    { label: 'Forums', href: '#forums' },
    { label: 'Culture', href: '#culture' },
    { label: 'Directory', href: '#directory' },
  ];

  return (
    <header className="sticky top-0 z-50 bg-white/95 backdrop-blur-sm border-b border-neutral-200">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16 md:h-20">
          {/* Logo */}
          <div className="flex items-center gap-3">
            <img src={logoImage} alt="LankaConnect Logo" className="h-14 w-14" />
            <div>
              <div className="text-neutral-900 text-xl">LankaConnect</div>
              <div className="text-neutral-500 text-xs hidden sm:block">Sri Lankan Community Hub</div>
            </div>
          </div>

          {/* Desktop Navigation */}
          <nav className="hidden lg:flex items-center gap-1">
            {navItems.map((item) => (
              <Button key={item.label} variant="ghost" className="text-neutral-600 hover:text-orange-600">
                {item.label}
              </Button>
            ))}
          </nav>

          {/* Search & Actions */}
          <div className="flex items-center gap-2 md:gap-3">
            <div className="hidden md:flex items-center relative max-w-xs">
              <Search className="absolute left-3 h-4 w-4 text-neutral-400" />
              <Input 
                placeholder="Search..." 
                className="pl-9 pr-4 w-64 bg-neutral-50 border-neutral-200"
              />
            </div>

            <Button variant="ghost" size="icon" className="md:hidden">
              <Search className="h-5 w-5 text-neutral-600" />
            </Button>

            <Button variant="ghost" size="icon" className="relative">
              <Bell className="h-5 w-5 text-neutral-600" />
              <Badge className="absolute -top-1 -right-1 h-5 w-5 flex items-center justify-center p-0 bg-orange-600 text-white text-xs">
                3
              </Badge>
            </Button>

            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="gap-2 hidden md:flex">
                  <Avatar className="h-8 w-8">
                    <AvatarImage src="" />
                    <AvatarFallback className="bg-gradient-to-br from-orange-500 to-rose-600 text-white">
                      PR
                    </AvatarFallback>
                  </Avatar>
                  <span className="text-neutral-700">Priya Rathnayake</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-56">
                <DropdownMenuItem>My Profile</DropdownMenuItem>
                <DropdownMenuItem>My Events</DropdownMenuItem>
                <DropdownMenuItem>My Listings</DropdownMenuItem>
                <DropdownMenuItem>Saved Items</DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem>Settings</DropdownMenuItem>
                <DropdownMenuItem>Help Center</DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem className="text-red-600">Logout</DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>

            {/* Mobile Menu */}
            <Sheet>
              <SheetTrigger asChild>
                <Button variant="ghost" size="icon" className="lg:hidden">
                  <Menu className="h-5 w-5" />
                </Button>
              </SheetTrigger>
              <SheetContent>
                <div className="flex flex-col gap-6 mt-8">
                  <div className="flex items-center gap-3 pb-6 border-b border-neutral-200">
                    <Avatar className="h-12 w-12">
                      <AvatarImage src="" />
                      <AvatarFallback className="bg-gradient-to-br from-orange-500 to-rose-600 text-white">
                        PR
                      </AvatarFallback>
                    </Avatar>
                    <div>
                      <div className="text-neutral-900">Priya Rathnayake</div>
                      <div className="text-neutral-500 text-sm">View Profile</div>
                    </div>
                  </div>
                  <nav className="flex flex-col gap-2">
                    {navItems.map((item) => (
                      <Button key={item.label} variant="ghost" className="justify-start text-neutral-600">
                        {item.label}
                      </Button>
                    ))}
                  </nav>
                </div>
              </SheetContent>
            </Sheet>
          </div>
        </div>
      </div>
    </header>
  );
}