FROM archlinux/archlinux:latest as run
USER root
RUN pacman -Sy
RUN pacman -S --noconfirm dotnet-runtime
RUN pacman -S --noconfirm ffmpeg
RUN pacman -S --noconfirm opencore-amr
