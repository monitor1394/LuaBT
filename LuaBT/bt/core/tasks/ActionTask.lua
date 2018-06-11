
local ActionTask = bt.Class("ActionTask",bt.Task)
bt.ActionTask = ActionTask

function ActionTask:ctor()
    bt.Task.ctor(self)
    self.name = "ActionTask"
    self.status = bt.Status.Resting
    self.startedTime = 0
    self.pausedTime = 0
    self.isPaused = false
    self.latch = false
end

function ActionTask:isRunning()
    return self.status == bt.Status.Running
end

function ActionTask:getElapsedTime()
    if self.isPaused then
        return self.pausedTime - self.startedTime
    end
    if self:isRunning() then
        return bt.time - self.startedTime
    end
    return 0
end

function ActionTask:executeAction(agent,blackborad)
    if not self.isActive then
        return bt.Status.Failure
    end
    if self.isPaused then
        self.startedTime = self.startedTime + bt.time - self.pausedTime
        self.isPaused = false
    end
    if self.status == bt.Status.Running then
        self:onUpdate()
        self.latch = false
        return self.status
    end
    if self.latch then
        self.latch = false
        return self.status
    end
    if not self:set(agent,blackborad) then
        return bt.Status.Failure
    end
    self.startedTime = bt.time
    self.status = bt.Status.Running
    self:onExecute()
    if self.status == bt.Status.Running then
        self:onUpdate()
    end
    self.latch = false
    return self.status
end

function ActionTask:endAction(success)
    if success ~= nil then
        self.latch = true
    else
        self.latch = false
    end
    self.isPaused = false
    if success == true then
        self.status = bt.Status.Success
    else
        self.status = bt.Status.Failure
    end
    self:onStop()
end

function ActionTask:pauseAction()
    if self.status ~= bt.Status.Running then
        return
    end
    self.pausedTime = bt.time
    self.isPaused = true
    self:onPause()
end

function ActionTask:onExecute()
end

function ActionTask:onUpdate()
end

function ActionTask:onStop()
end

function ActionTask:onPause()
end